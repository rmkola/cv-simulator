namespace Ocpp.Core.Messages;

/// <summary>Canonical OCPP 1.6 action names used as the CALL action string.</summary>
public static class OcppAction
{
    // Charge Point -> Central System (section 4)
    public const string Authorize = "Authorize";
    public const string BootNotification = "BootNotification";
    public const string DataTransfer = "DataTransfer";
    public const string DiagnosticsStatusNotification = "DiagnosticsStatusNotification";
    public const string FirmwareStatusNotification = "FirmwareStatusNotification";
    public const string Heartbeat = "Heartbeat";
    public const string MeterValues = "MeterValues";
    public const string StartTransaction = "StartTransaction";
    public const string StatusNotification = "StatusNotification";
    public const string StopTransaction = "StopTransaction";

    // Central System -> Charge Point (section 5)
    public const string CancelReservation = "CancelReservation";
    public const string ChangeAvailability = "ChangeAvailability";
    public const string ChangeConfiguration = "ChangeConfiguration";
    public const string ClearCache = "ClearCache";
    public const string ClearChargingProfile = "ClearChargingProfile";
    public const string GetCompositeSchedule = "GetCompositeSchedule";
    public const string GetConfiguration = "GetConfiguration";
    public const string GetDiagnostics = "GetDiagnostics";
    public const string GetLocalListVersion = "GetLocalListVersion";
    public const string RemoteStartTransaction = "RemoteStartTransaction";
    public const string RemoteStopTransaction = "RemoteStopTransaction";
    public const string ReserveNow = "ReserveNow";
    public const string Reset = "Reset";
    public const string SendLocalList = "SendLocalList";
    public const string SetChargingProfile = "SetChargingProfile";
    public const string TriggerMessage = "TriggerMessage";
    public const string UnlockConnector = "UnlockConnector";
    public const string UpdateFirmware = "UpdateFirmware";
}
