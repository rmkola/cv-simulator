using System.Text.Json.Nodes;
using Ocpp.Core.Messages;
using Ocpp.Core.Protocol;
using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>
/// The simulated OCPP 1.6 Charge Point. Owns the transport client, all domain state, the outgoing
/// (CP→CS) message operations, the automatic behaviors (heartbeat, meter values) and the incoming
/// (CS→CP) command handlers (see ChargePoint.Incoming.cs). Raises events for the UI to render;
/// the UI is responsible for marshaling them to its thread.
/// </summary>
public sealed partial class ChargePoint : IAsyncDisposable
{
    private readonly OcppRpcClient _client = new();
    private readonly object _sync = new();

    private CancellationTokenSource? _autoCts;
    private Task? _heartbeatLoop;
    private Task? _meterLoop;

    public ChargePointSettings Settings { get; }
    public ConfigurationStore Configuration { get; } = new();
    public LocalAuthList LocalAuthList { get; } = new();
    public ReservationStore Reservations { get; } = new();
    public ChargingProfileStore ChargingProfiles { get; } = new();

    public List<Connector> Connectors { get; } = new();

    public RegistrationStatus? LastBootStatus { get; private set; }
    public DiagnosticsStatus DiagnosticsStatus { get; private set; } = DiagnosticsStatus.Idle;
    public FirmwareStatus FirmwareStatus { get; private set; } = FirmwareStatus.Idle;

    public bool IsConnected => _client.IsConnected;

    /// <summary>Raised for every OCPP frame and local info line.</summary>
    public event Action<OcppLogEntry>? Log;

    /// <summary>Raised whenever domain state changes so the UI can re-render.</summary>
    public event Action? StateChanged;

    /// <summary>Raised for user-facing notifications (info/warnings).</summary>
    public event Action<string>? Notification;

    /// <summary>Raised when the transport connects (true) or disconnects (false).</summary>
    public event Action<bool>? ConnectionStateChanged;

    public ChargePoint(ChargePointSettings settings)
    {
        Settings = settings;
        _client.MessageLogged += e => Log?.Invoke(e);
        _client.ConnectionClosed += OnConnectionClosed;
        _client.IncomingCall = HandleIncomingAsync;
        RebuildConnectors();
    }

    /// <summary>Recreates the connector list (0..N) from <see cref="ChargePointSettings.NumberOfConnectors"/>.</summary>
    public void RebuildConnectors()
    {
        lock (_sync)
        {
            Connectors.Clear();
            for (int i = 0; i <= Math.Max(0, Settings.NumberOfConnectors); i++)
                Connectors.Add(new Connector(i) { ChargingPowerW = Settings.DefaultChargingPowerW });
        }
        var item = Configuration.All.FirstOrDefault(c => c.Key == "NumberOfConnectors");
        if (item is not null) item.Value = Settings.NumberOfConnectors.ToString();
        RaiseStateChanged();
    }

    public Connector? GetConnector(int id) => Connectors.FirstOrDefault(c => c.Id == id);

    // ---------------------------------------------------------------- connection lifecycle

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _client.PingInterval = TimeSpan.FromSeconds(Configuration.GetInt("WebSocketPingInterval", 60));
        await _client.ConnectAsync(Settings.BuildUri(), Settings.BasicAuthUser, Settings.BasicAuthPassword, ct)
            .ConfigureAwait(false);
        ConnectionStateChanged?.Invoke(true);

        if (Settings.AutoBootOnConnect)
        {
            try { await SendBootNotificationAsync(ct).ConfigureAwait(false); }
            catch (Exception ex) { Notify($"BootNotification failed: {ex.Message}"); }
        }
    }

    public async Task DisconnectAsync()
    {
        StopAutomation();
        await _client.DisconnectAsync().ConfigureAwait(false);
    }

    private void OnConnectionClosed(Exception? cause)
    {
        StopAutomation();
        ConnectionStateChanged?.Invoke(false);
    }

    // ---------------------------------------------------------------- outgoing messages (CP -> CS)

    public async Task<BootNotificationResponse> SendBootNotificationAsync(CancellationToken ct = default)
    {
        var req = new BootNotificationRequest
        {
            ChargePointVendor = Settings.ChargePointVendor,
            ChargePointModel = Settings.ChargePointModel,
            ChargePointSerialNumber = Settings.ChargePointSerialNumber,
            ChargeBoxSerialNumber = Settings.ChargeBoxSerialNumber,
            FirmwareVersion = Settings.FirmwareVersion,
            Iccid = Settings.Iccid,
            Imsi = Settings.Imsi,
            MeterType = Settings.MeterType,
            MeterSerialNumber = Settings.MeterSerialNumber,
        };

        var resp = await _client.CallAsync<BootNotificationResponse>(OcppAction.BootNotification, req, ct: ct)
            .ConfigureAwait(false);

        LastBootStatus = resp.Status;
        RaiseStateChanged();

        if (resp.Status == RegistrationStatus.Accepted)
        {
            if (resp.Interval > 0)
            {
                var item = Configuration.All.FirstOrDefault(c => c.Key == "HeartbeatInterval");
                if (item is not null) item.Value = resp.Interval.ToString();
            }
            StartAutomation();
            // Report initial connector statuses to the Central System.
            foreach (var c in Connectors.ToList())
                await SafeSend(() => SendStatusNotificationAsync(c.Id)).ConfigureAwait(false);
        }
        else
        {
            Notify($"BootNotification -> {resp.Status} (retry interval {resp.Interval}s).");
        }
        return resp;
    }

    public async Task SendHeartbeatAsync(CancellationToken ct = default)
    {
        var resp = await _client.CallAsync<HeartbeatResponse>(OcppAction.Heartbeat, new HeartbeatRequest(), ct: ct)
            .ConfigureAwait(false);
        _ = resp;
    }

    public async Task<AuthorizeResponse> AuthorizeAsync(string idTag, CancellationToken ct = default)
    {
        var resp = await _client.CallAsync<AuthorizeResponse>(OcppAction.Authorize, new AuthorizeRequest { IdTag = idTag }, ct: ct)
            .ConfigureAwait(false);
        Notify($"Authorize({idTag}) -> {resp.IdTagInfo.Status}.");
        return resp;
    }

    public async Task SendStatusNotificationAsync(int connectorId, CancellationToken ct = default)
    {
        var c = GetConnector(connectorId) ?? throw new ArgumentException($"Unknown connector {connectorId}.");
        var req = new StatusNotificationRequest
        {
            ConnectorId = connectorId,
            Status = c.Status,
            ErrorCode = c.ErrorCode,
            Timestamp = DateTimeOffset.UtcNow,
        };
        await _client.CallAsync<StatusNotificationResponse>(OcppAction.StatusNotification, req, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Starts a transaction: plugs in if needed, moves to Preparing, then sends StartTransaction.</summary>
    public async Task<StartTransactionResponse> StartTransactionAsync(int connectorId, string idTag, int? reservationId = null, CancellationToken ct = default)
    {
        var c = GetConnector(connectorId) ?? throw new ArgumentException($"Unknown connector {connectorId}.");
        if (connectorId == 0) throw new InvalidOperationException("Cannot start a transaction on connector 0.");
        if (c.ActiveTransaction is { IsActive: true })
            throw new InvalidOperationException($"Connector {connectorId} already has an active transaction.");

        c.CablePluggedIn = true;
        await SetConnectorStatusAsync(c, ChargePointStatus.Preparing).ConfigureAwait(false);

        var meterStart = (int)Math.Round(c.MeterWh);
        var req = new StartTransactionRequest
        {
            ConnectorId = connectorId,
            IdTag = idTag,
            MeterStart = meterStart,
            ReservationId = reservationId,
            Timestamp = DateTimeOffset.UtcNow,
        };

        var resp = await _client.CallAsync<StartTransactionResponse>(OcppAction.StartTransaction, req, ct: ct).ConfigureAwait(false);

        if (resp.IdTagInfo.Status == AuthorizationStatus.Accepted)
        {
            var tx = new Transaction(connectorId, idTag, meterStart, req.Timestamp) { TransactionId = resp.TransactionId };
            c.ActiveTransaction = tx;
            if (reservationId is int rid) { Reservations.Remove(rid); c.ReservationId = null; }
            await SetConnectorStatusAsync(c, ChargePointStatus.Charging).ConfigureAwait(false);
            Notify($"Transaction {resp.TransactionId} started on connector {connectorId}.");
        }
        else
        {
            await SetConnectorStatusAsync(c, ChargePointStatus.Available).ConfigureAwait(false);
            Notify($"StartTransaction rejected on connector {connectorId}: {resp.IdTagInfo.Status}.");
        }
        return resp;
    }

    /// <summary>Stops the active transaction on a connector.</summary>
    public async Task StopTransactionAsync(int connectorId, Reason reason = Reason.Local, CancellationToken ct = default)
    {
        var c = GetConnector(connectorId);
        if (c?.ActiveTransaction is not { IsActive: true } tx)
        {
            Notify($"No active transaction on connector {connectorId}.");
            return;
        }

        await SetConnectorStatusAsync(c, ChargePointStatus.Finishing).ConfigureAwait(false);

        var meterStop = (int)Math.Round(c.MeterWh);
        tx.MeterStop = meterStop;
        tx.StopTime = DateTimeOffset.UtcNow;

        var req = new StopTransactionRequest
        {
            TransactionId = tx.TransactionId,
            IdTag = tx.IdTag,
            MeterStop = meterStop,
            Timestamp = tx.StopTime.Value,
            Reason = reason,
        };
        await _client.CallAsync<StopTransactionResponse>(OcppAction.StopTransaction, req, ct: ct).ConfigureAwait(false);

        c.ActiveTransaction = null;
        c.CablePluggedIn = reason is not (Reason.EVDisconnected or Reason.UnlockCommand);
        var next = ApplyPendingAvailabilityOrDefault(c);
        await SetConnectorStatusAsync(c, next).ConfigureAwait(false);
        Notify($"Transaction {tx.TransactionId} stopped ({reason}).");
    }

    public async Task SendMeterValuesAsync(int connectorId, ReadingContext context = ReadingContext.SamplePeriodic, CancellationToken ct = default)
    {
        var c = GetConnector(connectorId);
        if (c is null) return;
        var req = new MeterValuesRequest
        {
            ConnectorId = connectorId,
            TransactionId = c.ActiveTransaction?.TransactionId,
            MeterValue = { BuildMeterValue(c, context) },
        };
        await _client.CallAsync<MeterValuesResponse>(OcppAction.MeterValues, req, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an arbitrary CP-initiated CALL with a hand-edited JSON payload and returns the raw
    /// response payload JSON. Powers the manual "Messages" tab.
    /// </summary>
    public async Task<string> SendRawAsync(string action, string payloadJson, CancellationToken ct = default)
    {
        JsonNode node;
        try { node = JsonNode.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson) ?? new JsonObject(); }
        catch (Exception ex) { throw new InvalidOperationException($"Geçersiz JSON: {ex.Message}", ex); }

        var frame = await _client.CallRawAsync(action, node, ct: ct).ConfigureAwait(false);
        return frame.Payload?.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) ?? "";
    }

    public async Task<DataTransferResponse> SendDataTransferAsync(string vendorId, string? messageId, string? data, CancellationToken ct = default)
    {
        var req = new DataTransferRequest { VendorId = vendorId, MessageId = messageId, Data = data };
        return await _client.CallAsync<DataTransferResponse>(OcppAction.DataTransfer, req, ct: ct).ConfigureAwait(false);
    }

    public async Task SendDiagnosticsStatusNotificationAsync(DiagnosticsStatus status, CancellationToken ct = default)
    {
        DiagnosticsStatus = status;
        RaiseStateChanged();
        await _client.CallAsync<DiagnosticsStatusNotificationResponse>(
            OcppAction.DiagnosticsStatusNotification, new DiagnosticsStatusNotificationRequest { Status = status }, ct: ct).ConfigureAwait(false);
    }

    public async Task SendFirmwareStatusNotificationAsync(FirmwareStatus status, CancellationToken ct = default)
    {
        FirmwareStatus = status;
        RaiseStateChanged();
        await _client.CallAsync<FirmwareStatusNotificationResponse>(
            OcppAction.FirmwareStatusNotification, new FirmwareStatusNotificationRequest { Status = status }, ct: ct).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------- connector helpers

    /// <summary>Simulates plugging a cable in on a connector (moves Available -> Preparing).</summary>
    public async Task PlugInAsync(int connectorId)
    {
        var c = GetConnector(connectorId);
        if (c is null || connectorId == 0) return;
        c.CablePluggedIn = true;
        if (c.Status is ChargePointStatus.Available)
            await SafeSend(() => SetConnectorStatusAsync(c, ChargePointStatus.Preparing)).ConfigureAwait(false);
    }

    /// <summary>Simulates unplugging the cable (stops any transaction, returns to Available).</summary>
    public async Task UnplugAsync(int connectorId)
    {
        var c = GetConnector(connectorId);
        if (c is null || connectorId == 0) return;

        if (c.ActiveTransaction is { IsActive: true } && Configuration.GetBool("StopTransactionOnEVSideDisconnect", true))
        {
            await SafeSend(() => StopTransactionAsync(connectorId, Reason.EVDisconnected)).ConfigureAwait(false);
            return;
        }
        c.CablePluggedIn = false;
        await SafeSend(() => SetConnectorStatusAsync(c, NormalStatusFor(c))).ConfigureAwait(false);
    }

    /// <summary>Sets a connector fault (or clears it) and notifies the Central System.
    /// Clearing (NoError) restores the connector to its normal status for its current state.</summary>
    public async Task SetFaultAsync(int connectorId, ChargePointErrorCode errorCode)
    {
        var c = GetConnector(connectorId);
        if (c is null) return;
        c.ErrorCode = errorCode;
        var status = errorCode == ChargePointErrorCode.NoError ? NormalStatusFor(c) : ChargePointStatus.Faulted;
        await SafeSend(() => SetConnectorStatusAsync(c, status)).ConfigureAwait(false);
    }

    /// <summary>The status a connector should report when it is not faulted, given its current state.</summary>
    private ChargePointStatus NormalStatusFor(Connector c) => c switch
    {
        { Availability: AvailabilityType.Inoperative } => ChargePointStatus.Unavailable,
        { ActiveTransaction: { IsActive: true } } => ChargePointStatus.Charging,
        { ReservationId: not null } => ChargePointStatus.Reserved,
        { CablePluggedIn: true } => ChargePointStatus.Preparing,
        _ => ChargePointStatus.Available,
    };

    private async Task SetConnectorStatusAsync(Connector c, ChargePointStatus status)
    {
        lock (_sync) c.Status = status;
        RaiseStateChanged();
        if (IsConnected)
            await SafeSend(() => SendStatusNotificationAsync(c.Id)).ConfigureAwait(false);
    }

    /// <summary>Status a connector should adopt right after a transaction ends. If the cable is still
    /// plugged it stays in Finishing until the driver unplugs (UnplugAsync); otherwise it is free.</summary>
    private ChargePointStatus ApplyPendingAvailabilityOrDefault(Connector c)
    {
        if (c.PendingAvailability is AvailabilityType pending)
        {
            c.Availability = pending;
            c.PendingAvailability = null;
        }
        if (c.Availability == AvailabilityType.Inoperative) return ChargePointStatus.Unavailable;
        return c.CablePluggedIn ? ChargePointStatus.Finishing : ChargePointStatus.Available;
    }

    private MeterValue BuildMeterValue(Connector c, ReadingContext context)
    {
        var power = c.IsCharging ? c.ChargingPowerW : 0;
        return new MeterValue
        {
            Timestamp = DateTimeOffset.UtcNow,
            SampledValue =
            {
                new SampledValue
                {
                    Value = ((int)Math.Round(c.MeterWh)).ToString(),
                    Measurand = Measurand.EnergyActiveImportRegister,
                    Unit = UnitOfMeasure.Wh, Context = context,
                },
                new SampledValue
                {
                    Value = power.ToString("0"),
                    Measurand = Measurand.PowerActiveImport, Unit = UnitOfMeasure.W, Context = context,
                },
                new SampledValue
                {
                    Value = (power / 230.0).ToString("0.0"),
                    Measurand = Measurand.CurrentImport, Unit = UnitOfMeasure.A, Context = context,
                },
                new SampledValue
                {
                    Value = "230.0",
                    Measurand = Measurand.Voltage, Unit = UnitOfMeasure.V, Context = context,
                },
                new SampledValue
                {
                    Value = ((int)Math.Round(c.StateOfChargePercent)).ToString(),
                    Measurand = Measurand.SoC, Unit = UnitOfMeasure.Percent, Context = context,
                },
            },
        };
    }

    // ---------------------------------------------------------------- automation (timers)

    private void StartAutomation()
    {
        StopAutomation();
        _autoCts = new CancellationTokenSource();
        _heartbeatLoop = Task.Run(() => HeartbeatLoopAsync(_autoCts.Token));
        _meterLoop = Task.Run(() => MeterLoopAsync(_autoCts.Token));
    }

    private void StopAutomation()
    {
        _autoCts?.Cancel();
        _autoCts = null;
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var interval = Math.Max(5, Configuration.GetInt("HeartbeatInterval", 300));
                await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);
                if (!IsConnected) continue;
                await SafeSend(() => SendHeartbeatAsync(ct)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task MeterLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var interval = Math.Max(1, Configuration.GetInt("MeterValueSampleInterval", 60));
                await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);

                foreach (var c in Connectors.ToList())
                {
                    if (!c.IsCharging) continue;
                    // Advance the simulated energy register and SoC.
                    c.MeterWh += c.ChargingPowerW * interval / 3600.0;
                    c.StateOfChargePercent = Math.Min(100, c.StateOfChargePercent + interval / 60.0);
                    RaiseStateChanged();

                    if (Settings.AutoMeterValues && IsConnected)
                        await SafeSend(() => SendMeterValuesAsync(c.Id, ReadingContext.SamplePeriodic, ct)).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    // ---------------------------------------------------------------- helpers

    private async Task SafeSend(Func<Task> action)
    {
        try { await action().ConfigureAwait(false); }
        catch (Exception ex) { Notify($"Send failed: {ex.Message}"); }
    }

    public void RaiseStateChanged() => StateChanged?.Invoke();
    internal void Notify(string message)
    {
        Log?.Invoke(OcppLogEntry.Information(message));
        Notification?.Invoke(message);
    }

    public async ValueTask DisposeAsync()
    {
        StopAutomation();
        await _client.DisposeAsync().ConfigureAwait(false);
    }
}
