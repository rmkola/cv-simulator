using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Ocpp.Core.Protocol;

/// <summary>
/// OCPP-J transport: a WebSocket client speaking the <c>ocpp1.6</c> sub-protocol. Handles CALL /
/// CALLRESULT / CALLERROR framing, request/response correlation by uniqueId, and dispatch of
/// incoming CALLs to a handler. Networking primitive only — it has no OCPP business logic.
/// </summary>
public sealed class OcppRpcClient : IAsyncDisposable
{
    public const string SubProtocol = "ocpp1.6";

    private readonly ConcurrentDictionary<string, PendingCall> _pending = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _loopCts;
    private Task? _receiveLoop;

    /// <summary>Default timeout applied to <see cref="CallAsync"/> when none is supplied.</summary>
    public TimeSpan DefaultCallTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>WebSocket-level keep-alive (ping) interval. Set before <see cref="ConnectAsync"/>.</summary>
    public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Handler for CALLs received from the peer. Returns the response payload (serialized into a
    /// CALLRESULT) or throws <see cref="OcppCallErrorException"/> to emit a CALLERROR.
    /// </summary>
    public Func<string, JsonNode?, CancellationToken, Task<object>>? IncomingCall { get; set; }

    /// <summary>Raised for every frame sent/received and for local info lines (marshal to UI thread yourself).</summary>
    public event Action<OcppLogEntry>? MessageLogged;

    /// <summary>Raised once when the connection closes (argument is the cause, or null for a clean close).</summary>
    public event Action<Exception?>? ConnectionClosed;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public async Task ConnectAsync(Uri uri, string? basicAuthUser, string? basicAuthPassword, CancellationToken ct = default)
    {
        if (IsConnected) throw new InvalidOperationException("Already connected.");

        var ws = new ClientWebSocket();
        ws.Options.AddSubProtocol(SubProtocol);
        ws.Options.KeepAliveInterval = PingInterval;

        if (!string.IsNullOrEmpty(basicAuthUser))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basicAuthUser}:{basicAuthPassword}"));
            ws.Options.SetRequestHeader("Authorization", $"Basic {token}");
        }

        Log(OcppLogEntry.Information($"Connecting to {uri} ..."));
        await ws.ConnectAsync(uri, ct).ConfigureAwait(false);

        _ws = ws;
        _loopCts = new CancellationTokenSource();
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_loopCts.Token));
        Log(OcppLogEntry.Information($"Connected (sub-protocol: {ws.SubProtocol ?? "none"})."));
    }

    /// <summary>Sends a CALL and awaits the correlated response, deserialized to <typeparamref name="TResult"/>.</summary>
    public async Task<TResult> CallAsync<TResult>(string action, object payload, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var frame = await CallRawAsync(action, payload, timeout, ct).ConfigureAwait(false);
        return frame.DeserializePayload<TResult>()
            ?? throw new OcppFormatException($"Empty/invalid CALLRESULT payload for {action}.");
    }

    /// <summary>Sends a CALL and returns the raw response frame (CALLRESULT).</summary>
    public async Task<OcppFrame> CallRawAsync(string action, object payload, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected.");

        var uniqueId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<OcppFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new PendingCall(action, tcs);
        _pending[uniqueId] = pending;

        var raw = OcppJson.SerializeCall(uniqueId, action, payload);
        try
        {
            await SendRawAsync(raw, ct).ConfigureAwait(false);
            Log(new OcppLogEntry(DateTimeOffset.UtcNow, LogDirection.Outgoing, MessageType.Call, action, uniqueId, raw));

            using var timeoutCts = new CancellationTokenSource(timeout ?? DefaultCallTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            using (linked.Token.Register(() => tcs.TrySetCanceled(linked.Token)))
            {
                var frame = await tcs.Task.ConfigureAwait(false);
                if (frame.MessageType == MessageType.CallError)
                    throw new OcppCallErrorException(frame.ErrorCode ?? OcppErrorCode.GenericError, frame.ErrorDescription ?? "");
                return frame;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"No response to {action} ({uniqueId}) within {(timeout ?? DefaultCallTimeout).TotalSeconds:0}s.");
        }
        finally
        {
            _pending.TryRemove(uniqueId, out _);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var ws = _ws!;
        var buffer = new byte[8192];
        Exception? cause = null;
        try
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
                        Log(OcppLogEntry.Information("Peer closed the connection."));
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var text = Encoding.UTF8.GetString(ms.ToArray());
                _ = HandleFrameAsync(text);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex) { cause = ex; Log(OcppLogEntry.Information($"Receive loop stopped: {ex.Message}")); }
        finally
        {
            FailAllPending(cause ?? new IOException("Connection closed."));
            ConnectionClosed?.Invoke(cause);
        }
    }

    private async Task HandleFrameAsync(string text)
    {
        OcppFrame frame;
        try { frame = OcppJson.ParseFrame(text); }
        catch (OcppFormatException ex) { Log(OcppLogEntry.Information($"Dropped malformed frame: {ex.Message}")); return; }

        Log(new OcppLogEntry(DateTimeOffset.UtcNow, LogDirection.Incoming, frame.MessageType, frame.Action, frame.UniqueId, text));

        switch (frame.MessageType)
        {
            case MessageType.CallResult:
            case MessageType.CallError:
                if (_pending.TryRemove(frame.UniqueId, out var pending))
                    pending.Completion.TrySetResult(frame);
                else
                    Log(OcppLogEntry.Information($"Unmatched response {frame.UniqueId} (no pending call)."));
                break;

            case MessageType.Call:
                await HandleIncomingCallAsync(frame).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleIncomingCallAsync(OcppFrame frame)
    {
        var handler = IncomingCall;
        if (handler is null)
        {
            await SendErrorAsync(frame.UniqueId, OcppErrorCode.NotImplemented, "No handler registered.").ConfigureAwait(false);
            return;
        }

        try
        {
            var response = await handler(frame.Action!, frame.Payload, CancellationToken.None).ConfigureAwait(false);
            var raw = OcppJson.SerializeCallResult(frame.UniqueId, response);
            await SendRawAsync(raw, CancellationToken.None).ConfigureAwait(false);
            Log(new OcppLogEntry(DateTimeOffset.UtcNow, LogDirection.Outgoing, MessageType.CallResult, frame.Action, frame.UniqueId, raw));
        }
        catch (OcppCallErrorException ex)
        {
            await SendErrorAsync(frame.UniqueId, ex.ErrorCode, ex.ErrorDescription, ex.ErrorDetailsJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SendErrorAsync(frame.UniqueId, OcppErrorCode.InternalError, ex.Message).ConfigureAwait(false);
        }
    }

    private async Task SendErrorAsync(string uniqueId, string code, string description, string detailsJson = "{}")
    {
        var raw = OcppJson.SerializeCallError(uniqueId, code, description, detailsJson);
        await SendRawAsync(raw, CancellationToken.None).ConfigureAwait(false);
        Log(new OcppLogEntry(DateTimeOffset.UtcNow, LogDirection.Outgoing, MessageType.CallError, null, uniqueId, raw));
    }

    private async Task SendRawAsync(string raw, CancellationToken ct)
    {
        var ws = _ws ?? throw new InvalidOperationException("Not connected.");
        var bytes = Encoding.UTF8.GetBytes(raw);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void FailAllPending(Exception ex)
    {
        foreach (var id in _pending.Keys)
            if (_pending.TryRemove(id, out var p))
                p.Completion.TrySetException(ex);
    }

    private void Log(OcppLogEntry entry) => MessageLogged?.Invoke(entry);

    public async Task DisconnectAsync()
    {
        _loopCts?.Cancel();
        if (_ws is { State: WebSocketState.Open })
        {
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false); }
            catch { /* ignore */ }
        }
        FailAllPending(new IOException("Disconnected."));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        try { if (_receiveLoop is not null) await _receiveLoop.ConfigureAwait(false); } catch { }
        _ws?.Dispose();
        _loopCts?.Dispose();
        _sendLock.Dispose();
    }

    private sealed record PendingCall(string Action, TaskCompletionSource<OcppFrame> Completion);
}
