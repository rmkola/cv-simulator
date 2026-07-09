using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Ocpp.Core.Protocol;

namespace Ocpp.TestServer;

/// <summary>
/// Server-side OCPP-J connection to one Charge Point: answers incoming CALLs and lets the operator
/// send CALLs to the Charge Point with response correlation. A trimmed mirror of OcppRpcClient.
/// </summary>
public sealed class ServerConnection
{
    private readonly WebSocket _ws;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<OcppFrame>> _pending = new();
    private readonly Func<string, JsonNode?, object> _onCall;

    public string ChargePointId { get; }

    public ServerConnection(string chargePointId, WebSocket ws, Func<string, JsonNode?, object> onCall)
    {
        ChargePointId = chargePointId;
        _ws = ws;
        _onCall = onCall;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    return;
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var text = Encoding.UTF8.GetString(ms.ToArray());
            await HandleAsync(text);
        }
    }

    private async Task HandleAsync(string text)
    {
        OcppFrame frame;
        try { frame = OcppJson.ParseFrame(text); }
        catch (OcppFormatException ex) { Console.WriteLine($"  [!] malformed frame: {ex.Message}"); return; }

        switch (frame.MessageType)
        {
            case MessageType.Call:
                Console.WriteLine($"  <- [{ChargePointId}] {frame.Action}  {Compact(frame.Payload)}");
                try
                {
                    var response = _onCall(frame.Action!, frame.Payload);
                    await SendRawAsync(OcppJson.SerializeCallResult(frame.UniqueId, response));
                }
                catch (Exception ex)
                {
                    await SendRawAsync(OcppJson.SerializeCallError(frame.UniqueId, OcppErrorCode.InternalError, ex.Message, "{}"));
                }
                break;

            case MessageType.CallResult:
            case MessageType.CallError:
                if (_pending.TryRemove(frame.UniqueId, out var tcs)) tcs.TrySetResult(frame);
                Console.WriteLine($"  -> [{ChargePointId}] {(frame.MessageType == MessageType.CallError ? "ERROR " + frame.ErrorCode : "result")}  {Compact(frame.Payload)}");
                break;
        }
    }

    public async Task<OcppFrame> CallAsync(string action, object payload, TimeSpan timeout)
    {
        var id = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<OcppFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;
        await SendRawAsync(OcppJson.SerializeCall(id, action, payload));
        Console.WriteLine($"  => [{ChargePointId}] {action} sent");

        using var cts = new CancellationTokenSource(timeout);
        using (cts.Token.Register(() => tcs.TrySetCanceled()))
            return await tcs.Task;
    }

    private async Task SendRawAsync(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        await _sendLock.WaitAsync();
        try { await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
        finally { _sendLock.Release(); }
    }

    private static string Compact(JsonNode? node)
    {
        var s = node?.ToJsonString() ?? "";
        return s.Length > 160 ? s[..160] + "…" : s;
    }
}
