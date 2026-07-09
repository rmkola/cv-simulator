using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using Ocpp.Core.Messages;
using Ocpp.Core.Protocol;
using Ocpp.Core.Types;
using Ocpp.TestServer;

// Minimal OCPP 1.6 Central System (CSMS) for exercising the simulator end-to-end.
// Accepts ocpp1.6 WebSocket connections and offers a console menu to send CS-initiated commands.

var prefix = args.Length > 0 ? args[0] : "http://localhost:9220/";
var connections = new ConcurrentDictionary<string, ServerConnection>();
var nextTransactionId = 1;

var listener = new HttpListener();
listener.Prefixes.Add(prefix);
listener.Start();

Console.WriteLine($"OCPP 1.6 Test CSMS listening on {prefix}");
Console.WriteLine($"Charge Points connect to: ws://{new Uri(prefix).Authority}/<ChargePointId>");
Console.WriteLine("Type 'help' for commands.\n");

_ = Task.Run(AcceptLoop);
RunConsole();
return;

async Task AcceptLoop()
{
    while (true)
    {
        HttpListenerContext ctx;
        try { ctx = await listener.GetContextAsync(); }
        catch { break; }

        if (!ctx.Request.IsWebSocketRequest)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            continue;
        }
        _ = Task.Run(() => HandleClient(ctx));
    }
}

async Task HandleClient(HttpListenerContext ctx)
{
    var cpId = ctx.Request.Url!.Segments.Last().Trim('/');
    if (string.IsNullOrEmpty(cpId)) cpId = "unknown";

    var wsCtx = await ctx.AcceptWebSocketAsync("ocpp1.6");
    Console.WriteLine($"\n[+] Charge Point connected: {cpId}");

    var conn = new ServerConnection(cpId, wsCtx.WebSocket, (action, payload) => HandleCall(cpId, action, payload));
    connections[cpId] = conn;
    try { await conn.RunAsync(CancellationToken.None); }
    catch (Exception ex) { Console.WriteLine($"[!] {cpId} error: {ex.Message}"); }
    finally { connections.TryRemove(cpId, out _); Console.WriteLine($"[-] Charge Point disconnected: {cpId}"); }
}

object HandleCall(string cpId, string action, JsonNode? payload) => action switch
{
    OcppAction.BootNotification => new BootNotificationResponse
    {
        CurrentTime = DateTimeOffset.UtcNow,
        Interval = 300,
        Status = RegistrationStatus.Accepted,
    },
    OcppAction.Heartbeat => new HeartbeatResponse { CurrentTime = DateTimeOffset.UtcNow },
    OcppAction.Authorize => new AuthorizeResponse { IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted } },
    OcppAction.StartTransaction => new StartTransactionResponse
    {
        TransactionId = Interlocked.Increment(ref nextTransactionId),
        IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted },
    },
    OcppAction.StopTransaction => new StopTransactionResponse { IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted } },
    OcppAction.DataTransfer => new DataTransferResponse { Status = DataTransferStatus.Accepted },
    OcppAction.StatusNotification => new StatusNotificationResponse(),
    OcppAction.MeterValues => new MeterValuesResponse(),
    OcppAction.DiagnosticsStatusNotification => new DiagnosticsStatusNotificationResponse(),
    OcppAction.FirmwareStatusNotification => new FirmwareStatusNotificationResponse(),
    _ => throw new OcppCallErrorException(OcppErrorCode.NotImplemented, $"CSMS does not handle {action}"),
};

void RunConsole()
{
    while (true)
    {
        var line = Console.ReadLine();
        if (line is null) { Task.Delay(500).Wait(); continue; }
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;

        try
        {
            switch (parts[0].ToLowerInvariant())
            {
                case "help": PrintHelp(); break;
                case "quit" or "exit": Environment.Exit(0); break;
                case "list":
                    Console.WriteLine(connections.IsEmpty ? "  (no connections)" : "  " + string.Join(", ", connections.Keys));
                    break;
                case "remotestart":
                    Send(parts[1], OcppAction.RemoteStartTransaction,
                        new RemoteStartTransactionRequest { ConnectorId = int.Parse(parts[2]), IdTag = parts[3] });
                    break;
                case "remotestop":
                    Send(parts[1], OcppAction.RemoteStopTransaction, new RemoteStopTransactionRequest { TransactionId = int.Parse(parts[2]) });
                    break;
                case "reset":
                    Send(parts[1], OcppAction.Reset, new ResetRequest { Type = Enum.Parse<ResetType>(parts[2], true) });
                    break;
                case "trigger":
                    Send(parts[1], OcppAction.TriggerMessage, new TriggerMessageRequest { RequestedMessage = Enum.Parse<MessageTrigger>(parts[2], true) });
                    break;
                case "getconf":
                    Send(parts[1], OcppAction.GetConfiguration, new GetConfigurationRequest());
                    break;
                case "setconf":
                    Send(parts[1], OcppAction.ChangeConfiguration, new ChangeConfigurationRequest { Key = parts[2], Value = parts[3] });
                    break;
                case "unlock":
                    Send(parts[1], OcppAction.UnlockConnector, new UnlockConnectorRequest { ConnectorId = int.Parse(parts[2]) });
                    break;
                case "changeavail":
                    Send(parts[1], OcppAction.ChangeAvailability,
                        new ChangeAvailabilityRequest { ConnectorId = int.Parse(parts[2]), Type = Enum.Parse<AvailabilityType>(parts[3], true) });
                    break;
                case "reservenow":
                    Send(parts[1], OcppAction.ReserveNow, new ReserveNowRequest
                    {
                        ConnectorId = int.Parse(parts[2]), IdTag = parts[3], ReservationId = int.Parse(parts[4]),
                        ExpiryDate = DateTimeOffset.UtcNow.AddHours(1),
                    });
                    break;
                case "cancelres":
                    Send(parts[1], OcppAction.CancelReservation, new CancelReservationRequest { ReservationId = int.Parse(parts[2]) });
                    break;
                case "getlocalversion":
                    Send(parts[1], OcppAction.GetLocalListVersion, new GetLocalListVersionRequest());
                    break;
                case "setprofile":
                    Send(parts[1], OcppAction.SetChargingProfile, SampleChargingProfile(int.Parse(parts[2])));
                    break;
                case "clearcache":
                    Send(parts[1], OcppAction.ClearCache, new ClearCacheRequest());
                    break;
                case "updatefw":
                    Send(parts[1], OcppAction.UpdateFirmware, new UpdateFirmwareRequest { Location = "http://example/fw.bin", RetrieveDate = DateTimeOffset.UtcNow });
                    break;
                case "getdiag":
                    Send(parts[1], OcppAction.GetDiagnostics, new GetDiagnosticsRequest { Location = "ftp://example/diag" });
                    break;
                default: Console.WriteLine("  unknown command; type 'help'"); break;
            }
        }
        catch (Exception ex) { Console.WriteLine($"  error: {ex.Message}"); }
    }
}

void Send(string cpId, string action, object payload)
{
    if (!connections.TryGetValue(cpId, out var conn)) { Console.WriteLine($"  no such connection: {cpId}"); return; }
    _ = Task.Run(async () =>
    {
        try { await conn.CallAsync(action, payload, TimeSpan.FromSeconds(30)); }
        catch (Exception ex) { Console.WriteLine($"  {action} failed: {ex.Message}"); }
    });
}

static SetChargingProfileRequest SampleChargingProfile(int connectorId) => new()
{
    ConnectorId = connectorId,
    CsChargingProfiles = new ChargingProfile
    {
        ChargingProfileId = 100, StackLevel = 1,
        ChargingProfilePurpose = ChargingProfilePurposeType.TxProfile,
        ChargingProfileKind = ChargingProfileKindType.Absolute,
        ChargingSchedule = new ChargingSchedule
        {
            ChargingRateUnit = ChargingRateUnitType.A,
            ChargingSchedulePeriod = { new ChargingSchedulePeriod { StartPeriod = 0, Limit = 16m, NumberPhases = 3 } },
        },
    },
};

static void PrintHelp()
{
    Console.WriteLine("""
      Commands (<cp> = Charge Point id):
        list
        remotestart <cp> <connectorId> <idTag>
        remotestop  <cp> <transactionId>
        reset       <cp> <Hard|Soft>
        trigger     <cp> <BootNotification|Heartbeat|StatusNotification|MeterValues|DiagnosticsStatusNotification|FirmwareStatusNotification>
        getconf     <cp>
        setconf     <cp> <key> <value>
        unlock      <cp> <connectorId>
        changeavail <cp> <connectorId> <Operative|Inoperative>
        reservenow  <cp> <connectorId> <idTag> <reservationId>
        cancelres   <cp> <reservationId>
        getlocalversion <cp>
        setprofile  <cp> <connectorId>
        clearcache  <cp>
        updatefw    <cp>
        getdiag     <cp>
        quit
      """);
}
