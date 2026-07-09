using System.Text.Json;
using System.Text.Json.Nodes;
using Ocpp.Core.Messages;
using Ocpp.Core.Protocol;
using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>
/// Incoming (Central System -> Charge Point) command handlers. Each returns the response payload
/// per spec; operations that also change behavior schedule the follow-up after the response is sent.
/// </summary>
public sealed partial class ChargePoint
{
    private Task<object> HandleIncomingAsync(string action, JsonNode? payload, CancellationToken ct)
    {
        object response = action switch
        {
            OcppAction.CancelReservation      => HandleCancelReservation(Req<CancelReservationRequest>(payload)),
            OcppAction.ChangeAvailability     => HandleChangeAvailability(Req<ChangeAvailabilityRequest>(payload)),
            OcppAction.ChangeConfiguration    => HandleChangeConfiguration(Req<ChangeConfigurationRequest>(payload)),
            OcppAction.ClearCache             => HandleClearCache(),
            OcppAction.ClearChargingProfile   => HandleClearChargingProfile(Req<ClearChargingProfileRequest>(payload)),
            OcppAction.DataTransfer           => HandleDataTransfer(Req<DataTransferRequest>(payload)),
            OcppAction.GetCompositeSchedule   => HandleGetCompositeSchedule(Req<GetCompositeScheduleRequest>(payload)),
            OcppAction.GetConfiguration       => HandleGetConfiguration(Req<GetConfigurationRequest>(payload)),
            OcppAction.GetDiagnostics         => HandleGetDiagnostics(Req<GetDiagnosticsRequest>(payload)),
            OcppAction.GetLocalListVersion    => HandleGetLocalListVersion(),
            OcppAction.RemoteStartTransaction => HandleRemoteStart(Req<RemoteStartTransactionRequest>(payload)),
            OcppAction.RemoteStopTransaction  => HandleRemoteStop(Req<RemoteStopTransactionRequest>(payload)),
            OcppAction.ReserveNow             => HandleReserveNow(Req<ReserveNowRequest>(payload)),
            OcppAction.Reset                  => HandleReset(Req<ResetRequest>(payload)),
            OcppAction.SendLocalList          => HandleSendLocalList(Req<SendLocalListRequest>(payload)),
            OcppAction.SetChargingProfile     => HandleSetChargingProfile(Req<SetChargingProfileRequest>(payload)),
            OcppAction.TriggerMessage         => HandleTriggerMessage(Req<TriggerMessageRequest>(payload)),
            OcppAction.UnlockConnector        => HandleUnlockConnector(Req<UnlockConnectorRequest>(payload)),
            OcppAction.UpdateFirmware         => HandleUpdateFirmware(Req<UpdateFirmwareRequest>(payload)),
            _ => throw new OcppCallErrorException(OcppErrorCode.NotImplemented, $"Action '{action}' is not supported."),
        };
        RaiseStateChanged();
        return Task.FromResult(response);
    }

    private static T Req<T>(JsonNode? payload) where T : new()
        => payload is null ? new T() : payload.Deserialize<T>(OcppJson.Options) ?? new T();

    /// <summary>Runs a follow-up action shortly after the response was returned to the peer.</summary>
    private void ScheduleFollowUp(Func<Task> action, int delayMs = 250)
        => _ = Task.Run(async () => { await Task.Delay(delayMs).ConfigureAwait(false); await SafeSend(action).ConfigureAwait(false); });

    // ---- Core ----

    private ChangeAvailabilityResponse HandleChangeAvailability(ChangeAvailabilityRequest r)
    {
        var targets = r.ConnectorId == 0 ? Connectors.ToList() : new List<Connector> { GetConnector(r.ConnectorId)! };
        if (targets.Any(t => t is null)) return new ChangeAvailabilityResponse { Status = AvailabilityStatus.Rejected };

        bool scheduled = false;
        foreach (var c in targets)
        {
            if (c.Id == 0) { c.Availability = r.Type; continue; }
            if (c.ActiveTransaction is { IsActive: true })
            {
                c.PendingAvailability = r.Type; // apply when the transaction ends
                scheduled = true;
            }
            else
            {
                c.Availability = r.Type;
                var status = r.Type == AvailabilityType.Inoperative ? ChargePointStatus.Unavailable : ChargePointStatus.Available;
                ScheduleFollowUp(() => SetConnectorStatusAsync(c, status));
            }
        }
        return new ChangeAvailabilityResponse { Status = scheduled ? AvailabilityStatus.Scheduled : AvailabilityStatus.Accepted };
    }

    private ChangeConfigurationResponse HandleChangeConfiguration(ChangeConfigurationRequest r)
    {
        var status = Configuration.Set(r.Key, r.Value);
        if (status == ConfigurationStatus.Accepted && string.Equals(r.Key, "WebSocketPingInterval", StringComparison.OrdinalIgnoreCase))
            _client.PingInterval = TimeSpan.FromSeconds(Configuration.GetInt("WebSocketPingInterval", 60));
        return new ChangeConfigurationResponse { Status = status };
    }

    private ClearCacheResponse HandleClearCache()
        => new() { Status = ClearCacheStatus.Accepted };

    private DataTransferResponse HandleDataTransfer(DataTransferRequest r)
    {
        Notify($"DataTransfer from CS (vendor '{r.VendorId}', message '{r.MessageId}').");
        return new DataTransferResponse { Status = DataTransferStatus.Accepted };
    }

    private GetConfigurationResponse HandleGetConfiguration(GetConfigurationRequest r)
    {
        var (known, unknown) = Configuration.Query(r.Key);
        return new GetConfigurationResponse
        {
            ConfigurationKey = known.Count > 0 ? known : null,
            UnknownKey = unknown.Count > 0 ? unknown : null,
        };
    }

    private RemoteStartTransactionResponse HandleRemoteStart(RemoteStartTransactionRequest r)
    {
        var connector = r.ConnectorId is int id
            ? GetConnector(id)
            : Connectors.FirstOrDefault(c => c.Id != 0 && c.ActiveTransaction is null && c.Availability == AvailabilityType.Operative);

        if (connector is null || connector.Id == 0 || connector.ActiveTransaction is { IsActive: true } ||
            connector.Availability == AvailabilityType.Inoperative)
            return new RemoteStartTransactionResponse { Status = RemoteStartStopStatus.Rejected };

        if (r.ChargingProfile is not null)
            ChargingProfiles.Set(connector.Id, r.ChargingProfile);

        ScheduleFollowUp(() => StartTransactionAsync(connector.Id, r.IdTag));
        return new RemoteStartTransactionResponse { Status = RemoteStartStopStatus.Accepted };
    }

    private RemoteStopTransactionResponse HandleRemoteStop(RemoteStopTransactionRequest r)
    {
        var connector = Connectors.FirstOrDefault(c => c.ActiveTransaction is { IsActive: true } tx && tx.TransactionId == r.TransactionId);
        if (connector is null)
            return new RemoteStopTransactionResponse { Status = RemoteStartStopStatus.Rejected };

        ScheduleFollowUp(() => StopTransactionAsync(connector.Id, Reason.Remote));
        return new RemoteStopTransactionResponse { Status = RemoteStartStopStatus.Accepted };
    }

    private ResetResponse HandleReset(ResetRequest r)
    {
        ScheduleFollowUp(() => PerformResetAsync(r.Type), delayMs: 300);
        return new ResetResponse { Status = ResetStatus.Accepted };
    }

    private async Task PerformResetAsync(ResetType type)
    {
        var reason = type == ResetType.Hard ? Reason.HardReset : Reason.SoftReset;
        foreach (var c in Connectors.ToList())
            if (c.ActiveTransaction is { IsActive: true })
                await SafeSend(() => StopTransactionAsync(c.Id, reason)).ConfigureAwait(false);

        Notify($"{type} reset performed; rebooting.");
        await Task.Delay(500).ConfigureAwait(false);
        if (IsConnected)
            await SafeSend(() => SendBootNotificationAsync()).ConfigureAwait(false);
    }

    private UnlockConnectorResponse HandleUnlockConnector(UnlockConnectorRequest r)
    {
        var c = GetConnector(r.ConnectorId);
        if (c is null || c.Id == 0)
            return new UnlockConnectorResponse { Status = UnlockStatus.NotSupported };

        ScheduleFollowUp(async () =>
        {
            if (c.ActiveTransaction is { IsActive: true })
                await StopTransactionAsync(c.Id, Reason.UnlockCommand).ConfigureAwait(false);
            c.CablePluggedIn = false;
            await SetConnectorStatusAsync(c, ChargePointStatus.Available).ConfigureAwait(false);
        });
        return new UnlockConnectorResponse { Status = UnlockStatus.Unlocked };
    }

    // ---- Firmware Management ----

    private GetDiagnosticsResponse HandleGetDiagnostics(GetDiagnosticsRequest r)
    {
        var fileName = $"diagnostics-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.log";
        ScheduleFollowUp(async () =>
        {
            await SendDiagnosticsStatusNotificationAsync(DiagnosticsStatus.Uploading).ConfigureAwait(false);
            await Task.Delay(1500).ConfigureAwait(false);
            await SendDiagnosticsStatusNotificationAsync(DiagnosticsStatus.Uploaded).ConfigureAwait(false);
        });
        return new GetDiagnosticsResponse { FileName = fileName };
    }

    private UpdateFirmwareResponse HandleUpdateFirmware(UpdateFirmwareRequest r)
    {
        ScheduleFollowUp(async () =>
        {
            foreach (var s in new[] { FirmwareStatus.Downloading, FirmwareStatus.Downloaded, FirmwareStatus.Installing, FirmwareStatus.Installed })
            {
                await SendFirmwareStatusNotificationAsync(s).ConfigureAwait(false);
                await Task.Delay(1200).ConfigureAwait(false);
            }
        });
        return new UpdateFirmwareResponse();
    }

    // ---- Local Auth List Management ----

    private GetLocalListVersionResponse HandleGetLocalListVersion()
        => new() { ListVersion = LocalAuthList.Version };

    private SendLocalListResponse HandleSendLocalList(SendLocalListRequest r)
    {
        if (!Configuration.GetBool("LocalAuthListEnabled", true))
            return new SendLocalListResponse { Status = UpdateStatus.NotSupported };

        var status = LocalAuthList.Apply(r.ListVersion, r.UpdateType, r.LocalAuthorizationList);
        return new SendLocalListResponse { Status = status };
    }

    // ---- Reservation ----

    private ReserveNowResponse HandleReserveNow(ReserveNowRequest r)
    {
        var c = GetConnector(r.ConnectorId);
        if (c is null)
            return new ReserveNowResponse { Status = ReservationStatus.Rejected };
        if (c.ErrorCode != ChargePointErrorCode.NoError || c.Status == ChargePointStatus.Faulted)
            return new ReserveNowResponse { Status = ReservationStatus.Faulted };
        if (c.Availability == AvailabilityType.Inoperative || c.Status == ChargePointStatus.Unavailable)
            return new ReserveNowResponse { Status = ReservationStatus.Unavailable };
        if (c.ActiveTransaction is { IsActive: true } || c.Status == ChargePointStatus.Charging)
            return new ReserveNowResponse { Status = ReservationStatus.Occupied };

        Reservations.AddOrReplace(new Reservation
        {
            ReservationId = r.ReservationId,
            ConnectorId = r.ConnectorId,
            IdTag = r.IdTag,
            ParentIdTag = r.ParentIdTag,
            ExpiryDate = r.ExpiryDate,
        });
        c.ReservationId = r.ReservationId;
        if (c.Id != 0)
            ScheduleFollowUp(() => SetConnectorStatusAsync(c, ChargePointStatus.Reserved));
        return new ReserveNowResponse { Status = ReservationStatus.Accepted };
    }

    private CancelReservationResponse HandleCancelReservation(CancelReservationRequest r)
    {
        if (!Reservations.TryGet(r.ReservationId, out var res))
            return new CancelReservationResponse { Status = CancelReservationStatus.Rejected };

        Reservations.Remove(r.ReservationId);
        var c = GetConnector(res.ConnectorId);
        if (c is not null)
        {
            c.ReservationId = null;
            if (c.Status == ChargePointStatus.Reserved)
                ScheduleFollowUp(() => SetConnectorStatusAsync(c, ChargePointStatus.Available));
        }
        return new CancelReservationResponse { Status = CancelReservationStatus.Accepted };
    }

    // ---- Smart Charging ----

    private SetChargingProfileResponse HandleSetChargingProfile(SetChargingProfileRequest r)
    {
        var profile = r.CsChargingProfiles;
        var maxStack = Configuration.GetInt("ChargeProfileMaxStackLevel", 10);
        if (profile.StackLevel > maxStack)
            return new SetChargingProfileResponse { Status = ChargingProfileStatus.Rejected };
        if (profile.ChargingProfilePurpose == ChargingProfilePurposeType.TxProfile && r.ConnectorId == 0)
            return new SetChargingProfileResponse { Status = ChargingProfileStatus.Rejected };

        ChargingProfiles.Set(r.ConnectorId, profile);
        return new SetChargingProfileResponse { Status = ChargingProfileStatus.Accepted };
    }

    private ClearChargingProfileResponse HandleClearChargingProfile(ClearChargingProfileRequest r)
    {
        var cleared = ChargingProfiles.Clear(r.Id, r.ConnectorId, r.ChargingProfilePurpose, r.StackLevel);
        return new ClearChargingProfileResponse { Status = cleared ? ClearChargingProfileStatus.Accepted : ClearChargingProfileStatus.Unknown };
    }

    private GetCompositeScheduleResponse HandleGetCompositeSchedule(GetCompositeScheduleRequest r)
    {
        var installed = ChargingProfiles.All
            .Where(p => p.ConnectorId == r.ConnectorId || p.ConnectorId == 0)
            .OrderByDescending(p => p.Profile.StackLevel)
            .FirstOrDefault();

        if (installed is null)
            return new GetCompositeScheduleResponse { Status = GetCompositeScheduleStatus.Rejected };

        return new GetCompositeScheduleResponse
        {
            Status = GetCompositeScheduleStatus.Accepted,
            ConnectorId = r.ConnectorId,
            ScheduleStart = DateTimeOffset.UtcNow,
            ChargingSchedule = installed.Profile.ChargingSchedule,
        };
    }

    // ---- Remote Trigger ----

    private TriggerMessageResponse HandleTriggerMessage(TriggerMessageRequest r)
    {
        Func<Task>? job = r.RequestedMessage switch
        {
            MessageTrigger.BootNotification              => () => SendBootNotificationAsync(),
            MessageTrigger.Heartbeat                     => () => SendHeartbeatAsync(),
            MessageTrigger.StatusNotification            => () => TriggerStatusNotificationAsync(r.ConnectorId),
            MessageTrigger.MeterValues                   => () => TriggerMeterValuesAsync(r.ConnectorId),
            MessageTrigger.DiagnosticsStatusNotification => () => SendDiagnosticsStatusNotificationAsync(DiagnosticsStatus),
            MessageTrigger.FirmwareStatusNotification    => () => SendFirmwareStatusNotificationAsync(FirmwareStatus),
            _ => null,
        };

        if (job is null)
            return new TriggerMessageResponse { Status = TriggerMessageStatus.NotImplemented };

        ScheduleFollowUp(job);
        return new TriggerMessageResponse { Status = TriggerMessageStatus.Accepted };
    }

    private async Task TriggerStatusNotificationAsync(int? connectorId)
    {
        var targets = connectorId is int id ? new[] { id } : Connectors.Select(c => c.Id).ToArray();
        foreach (var i in targets)
            await SafeSend(() => SendStatusNotificationAsync(i)).ConfigureAwait(false);
    }

    private async Task TriggerMeterValuesAsync(int? connectorId)
    {
        var targets = connectorId is int id ? new[] { id } : Connectors.Where(c => c.Id != 0).Select(c => c.Id).ToArray();
        foreach (var i in targets)
            await SafeSend(() => SendMeterValuesAsync(i, ReadingContext.Trigger)).ConfigureAwait(false);
    }
}
