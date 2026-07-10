using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Ocpp.Core.Domain;
using Ocpp.Core.Messages;
using Ocpp.Core.Protocol;
using Ocpp.Core.Types;
using Xunit;

namespace Ocpp.Core.Tests;

/// <summary>
/// End-to-end tests: a real in-process CSMS (HttpListener + WebSocket, ocpp1.6) driving a full
/// ChargePoint through boot, transactions and a CS-initiated command.
/// </summary>
public sealed class IntegrationTests : IAsyncLifetime
{
    private readonly int _port = 9331;
    private TestCsms _csms = null!;

    public Task InitializeAsync()
    {
        _csms = new TestCsms(_port);
        _csms.Start();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _csms.DisposeAsync();

    private ChargePoint NewChargePoint() => new(new ChargePointSettings
    {
        CentralSystemUrl = $"ws://localhost:{_port}/",
        ChargePointId = "TESTCP",
        NumberOfConnectors = 2,
        AutoBootOnConnect = true,
        AutoMeterValues = false,
    });

    private static async Task<bool> WaitFor(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(50);
        }
        return condition();
    }

    [Fact]
    public async Task Connect_AutoBoot_IsAccepted()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();

        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));
        Assert.True(cp.IsConnected);
    }

    [Fact]
    public async Task StartAndStopTransaction_UpdatesConnectorState()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        var resp = await cp.StartTransactionAsync(1, "TAG-1");
        Assert.Equal(AuthorizationStatus.Accepted, resp.IdTagInfo.Status);
        Assert.True(resp.TransactionId > 0);
        Assert.Equal(ChargePointStatus.Charging, cp.GetConnector(1)!.Status);

        // Stopping leaves the connector in Finishing with the cable still plugged.
        await cp.StopTransactionAsync(1, Reason.Local);
        Assert.True(await WaitFor(() => cp.GetConnector(1)!.Status == ChargePointStatus.Finishing));
        Assert.Null(cp.GetConnector(1)!.ActiveTransaction);
        Assert.True(cp.GetConnector(1)!.CablePluggedIn);

        // Only unplugging frees the connector back to Available.
        await cp.UnplugAsync(1);
        Assert.True(await WaitFor(() => cp.GetConnector(1)!.Status == ChargePointStatus.Available));
        Assert.False(cp.GetConnector(1)!.CablePluggedIn);
    }

    [Fact]
    public async Task StartTransaction_Timestamp_IsUtc_NotLocalOffset()
    {
        await using var cp = NewChargePoint();

        // Capture the raw outgoing StartTransaction frame.
        string? startRaw = null;
        cp.Log += e =>
        {
            if (e is { Direction: LogDirection.Outgoing, Action: OcppAction.StartTransaction })
                startRaw = e.Raw;
        };

        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        var beforeUtc = DateTimeOffset.UtcNow;
        await cp.StartTransactionAsync(1, "TAG-1");
        var afterUtc = DateTimeOffset.UtcNow;

        Assert.NotNull(startRaw);
        var payload = OcppJson.ParseFrame(startRaw!).Payload!;
        var sent = payload["timestamp"]!.GetValue<DateTimeOffset>();

        // The sent instant must equal current UTC (±1 min), i.e. NOT shifted by a local offset (e.g. +3h).
        Assert.InRange(sent.ToUniversalTime(),
            beforeUtc.AddMinutes(-1), afterUtc.AddMinutes(1));
        Assert.Equal(TimeSpan.Zero, sent.Offset); // serialized as UTC (+00:00)
    }

    [Fact]
    public async Task StopTransaction_StaysFinishing_UntilUnplugged()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        await cp.StartTransactionAsync(1, "TAG-1");
        await cp.StopTransactionAsync(1, Reason.Local);
        Assert.Equal(ChargePointStatus.Finishing, cp.GetConnector(1)!.Status);
        Assert.True(cp.GetConnector(1)!.CablePluggedIn);

        // Wait well past any scheduled follow-up (7s): it must STAY Finishing — nothing auto-unplugs.
        await Task.Delay(7000);
        Assert.Equal(ChargePointStatus.Finishing, cp.GetConnector(1)!.Status);
        Assert.True(cp.GetConnector(1)!.CablePluggedIn);
    }

    [Fact]
    public async Task RemoteStartTransaction_FromCsms_StartsCharging()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        var frame = await _csms.CallChargePointAsync("TESTCP", OcppAction.RemoteStartTransaction,
            new RemoteStartTransactionRequest { ConnectorId = 1, IdTag = "REMOTE-TAG" });
        var result = frame.DeserializePayload<RemoteStartTransactionResponse>()!;

        Assert.Equal(RemoteStartStopStatus.Accepted, result.Status);
        Assert.True(await WaitFor(() => cp.GetConnector(1)!.Status == ChargePointStatus.Charging));
    }

    [Fact]
    public async Task GetConfiguration_FromCsms_ReturnsAllKeys()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        var frame = await _csms.CallChargePointAsync("TESTCP", OcppAction.GetConfiguration, new GetConfigurationRequest());
        var result = frame.DeserializePayload<GetConfigurationResponse>()!;

        Assert.NotNull(result.ConfigurationKey);
        Assert.Equal(43, result.ConfigurationKey!.Count);
        Assert.Contains(result.ConfigurationKey, k => k.Key == "HeartbeatInterval");
    }

    [Fact]
    public async Task Reset_FromCsms_IsAcceptedAndReboots()
    {
        await using var cp = NewChargePoint();
        await cp.ConnectAsync();
        Assert.True(await WaitFor(() => cp.LastBootStatus == RegistrationStatus.Accepted));

        var bootCountBefore = _csms.BootCount;
        var frame = await _csms.CallChargePointAsync("TESTCP", OcppAction.Reset, new ResetRequest { Type = ResetType.Soft });
        var result = frame.DeserializePayload<ResetResponse>()!;

        Assert.Equal(ResetStatus.Accepted, result.Status);
        Assert.True(await WaitFor(() => _csms.BootCount > bootCountBefore)); // rebooted -> new BootNotification
    }

    // ---- In-process CSMS ----

    private sealed class TestCsms : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly ConcurrentDictionary<string, WsPeer> _peers = new();
        private int _txId = 1000;
        public int BootCount;

        public TestCsms(int port) => _listener.Prefixes.Add($"http://localhost:{port}/");

        public void Start()
        {
            _listener.Start();
            _ = Task.Run(AcceptLoop);
        }

        private async Task AcceptLoop()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { break; }
                if (!ctx.Request.IsWebSocketRequest) { ctx.Response.StatusCode = 400; ctx.Response.Close(); continue; }

                var cpId = ctx.Request.Url!.Segments.Last().Trim('/');
                var wsCtx = await ctx.AcceptWebSocketAsync("ocpp1.6");
                var peer = new WsPeer(wsCtx.WebSocket, this);
                _peers[cpId] = peer;
                _ = Task.Run(() => peer.RunAsync());
            }
        }

        public async Task<OcppFrame> CallChargePointAsync(string cpId, string action, object payload)
        {
            var peer = await WaitPeer(cpId);
            return await peer.CallAsync(action, payload);
        }

        private async Task<WsPeer> WaitPeer(string cpId)
        {
            for (int i = 0; i < 100 && !_peers.ContainsKey(cpId); i++) await Task.Delay(50);
            return _peers[cpId];
        }

        internal object Respond(string action) => action switch
        {
            OcppAction.BootNotification => Boot(),
            OcppAction.Heartbeat => new HeartbeatResponse { CurrentTime = DateTimeOffset.UtcNow },
            OcppAction.Authorize => new AuthorizeResponse { IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted } },
            OcppAction.StartTransaction => new StartTransactionResponse { TransactionId = Interlocked.Increment(ref _txId), IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted } },
            OcppAction.StopTransaction => new StopTransactionResponse { IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted } },
            OcppAction.DataTransfer => new DataTransferResponse { Status = DataTransferStatus.Accepted },
            _ => EmptyOk(action),
        };

        private BootNotificationResponse Boot()
        {
            Interlocked.Increment(ref BootCount);
            return new BootNotificationResponse { CurrentTime = DateTimeOffset.UtcNow, Interval = 300, Status = RegistrationStatus.Accepted };
        }

        private static object EmptyOk(string action) => action switch
        {
            OcppAction.StatusNotification => new StatusNotificationResponse(),
            OcppAction.MeterValues => new MeterValuesResponse(),
            OcppAction.DiagnosticsStatusNotification => new DiagnosticsStatusNotificationResponse(),
            OcppAction.FirmwareStatusNotification => new FirmwareStatusNotificationResponse(),
            _ => throw new OcppCallErrorException(OcppErrorCode.NotImplemented, action),
        };

        public async ValueTask DisposeAsync()
        {
            try { _listener.Stop(); _listener.Close(); } catch { }
            await Task.CompletedTask;
        }

        private sealed class WsPeer
        {
            private readonly WebSocket _ws;
            private readonly TestCsms _owner;
            private readonly SemaphoreSlim _send = new(1, 1);
            private readonly ConcurrentDictionary<string, TaskCompletionSource<OcppFrame>> _pending = new();

            public WsPeer(WebSocket ws, TestCsms owner) { _ws = ws; _owner = owner; }

            public async Task RunAsync()
            {
                var buffer = new byte[8192];
                while (_ws.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (r.MessageType == WebSocketMessageType.Close) return;
                        ms.Write(buffer, 0, r.Count);
                    } while (!r.EndOfMessage);

                    var frame = OcppJson.ParseFrame(Encoding.UTF8.GetString(ms.ToArray()));
                    if (frame.MessageType == MessageType.Call)
                    {
                        var resp = _owner.Respond(frame.Action!);
                        await SendAsync(OcppJson.SerializeCallResult(frame.UniqueId, resp));
                    }
                    else if (_pending.TryRemove(frame.UniqueId, out var tcs))
                    {
                        tcs.TrySetResult(frame);
                    }
                }
            }

            public async Task<OcppFrame> CallAsync(string action, object payload)
            {
                var id = Guid.NewGuid().ToString("N");
                var tcs = new TaskCompletionSource<OcppFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pending[id] = tcs;
                await SendAsync(OcppJson.SerializeCall(id, action, payload));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                    return await tcs.Task;
            }

            private async Task SendAsync(string raw)
            {
                var bytes = Encoding.UTF8.GetBytes(raw);
                await _send.WaitAsync();
                try { await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
                finally { _send.Release(); }
            }
        }
    }
}
