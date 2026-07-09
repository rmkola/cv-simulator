using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Core profile messages (spec 6.x). Optional fields are nullable and omitted when null. ----

// 6.1 / 6.2 Authorize
public sealed class AuthorizeRequest
{
    public string IdTag { get; set; } = "";
}

public sealed class AuthorizeResponse
{
    public IdTagInfo IdTagInfo { get; set; } = new();
}

// 6.3 / 6.4 BootNotification
public sealed class BootNotificationRequest
{
    public string? ChargeBoxSerialNumber { get; set; }
    public string ChargePointModel { get; set; } = "";
    public string? ChargePointSerialNumber { get; set; }
    public string ChargePointVendor { get; set; } = "";
    public string? FirmwareVersion { get; set; }
    public string? Iccid { get; set; }
    public string? Imsi { get; set; }
    public string? MeterSerialNumber { get; set; }
    public string? MeterType { get; set; }
}

public sealed class BootNotificationResponse
{
    public DateTimeOffset CurrentTime { get; set; }
    public int Interval { get; set; }
    public RegistrationStatus Status { get; set; }
}

// 6.15 / 6.16 DataTransfer (bidirectional)
public sealed class DataTransferRequest
{
    public string VendorId { get; set; } = "";
    public string? MessageId { get; set; }
    public string? Data { get; set; }
}

public sealed class DataTransferResponse
{
    public DataTransferStatus Status { get; set; }
    public string? Data { get; set; }
}

// 6.29 / 6.30 Heartbeat
public sealed class HeartbeatRequest { }

public sealed class HeartbeatResponse
{
    public DateTimeOffset CurrentTime { get; set; }
}

// 6.31 / 6.32 MeterValues
public sealed class MeterValuesRequest
{
    public int ConnectorId { get; set; }
    public int? TransactionId { get; set; }
    public List<MeterValue> MeterValue { get; set; } = new();
}

public sealed class MeterValuesResponse { }

// 6.45 / 6.46 StartTransaction
public sealed class StartTransactionRequest
{
    public int ConnectorId { get; set; }
    public string IdTag { get; set; } = "";
    public int MeterStart { get; set; }
    public int? ReservationId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class StartTransactionResponse
{
    public IdTagInfo IdTagInfo { get; set; } = new();
    public int TransactionId { get; set; }
}

// 6.47 / 6.48 StatusNotification
public sealed class StatusNotificationRequest
{
    public int ConnectorId { get; set; }
    public ChargePointErrorCode ErrorCode { get; set; }
    public string? Info { get; set; }
    public ChargePointStatus Status { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? VendorId { get; set; }
    public string? VendorErrorCode { get; set; }
}

public sealed class StatusNotificationResponse { }

// 6.49 / 6.50 StopTransaction
public sealed class StopTransactionRequest
{
    public string? IdTag { get; set; }
    public int MeterStop { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int TransactionId { get; set; }
    public Reason? Reason { get; set; }
    public List<MeterValue>? TransactionData { get; set; }
}

public sealed class StopTransactionResponse
{
    public IdTagInfo? IdTagInfo { get; set; }
}

// 6.7 / 6.8 ChangeAvailability
public sealed class ChangeAvailabilityRequest
{
    public int ConnectorId { get; set; }
    public AvailabilityType Type { get; set; }
}

public sealed class ChangeAvailabilityResponse
{
    public AvailabilityStatus Status { get; set; }
}

// 6.9 / 6.10 ChangeConfiguration
public sealed class ChangeConfigurationRequest
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class ChangeConfigurationResponse
{
    public ConfigurationStatus Status { get; set; }
}

// 6.11 / 6.12 ClearCache
public sealed class ClearCacheRequest { }

public sealed class ClearCacheResponse
{
    public ClearCacheStatus Status { get; set; }
}

// 6.23 / 6.24 GetConfiguration
public sealed class GetConfigurationRequest
{
    public List<string>? Key { get; set; }
}

public sealed class GetConfigurationResponse
{
    public List<KeyValue>? ConfigurationKey { get; set; }
    public List<string>? UnknownKey { get; set; }
}

// 6.33 / 6.34 RemoteStartTransaction
public sealed class RemoteStartTransactionRequest
{
    public int? ConnectorId { get; set; }
    public string IdTag { get; set; } = "";
    public ChargingProfile? ChargingProfile { get; set; }
}

public sealed class RemoteStartTransactionResponse
{
    public RemoteStartStopStatus Status { get; set; }
}

// 6.35 / 6.36 RemoteStopTransaction
public sealed class RemoteStopTransactionRequest
{
    public int TransactionId { get; set; }
}

public sealed class RemoteStopTransactionResponse
{
    public RemoteStartStopStatus Status { get; set; }
}

// 6.39 / 6.40 Reset
public sealed class ResetRequest
{
    public ResetType Type { get; set; }
}

public sealed class ResetResponse
{
    public ResetStatus Status { get; set; }
}

// 6.53 / 6.54 UnlockConnector
public sealed class UnlockConnectorRequest
{
    public int ConnectorId { get; set; }
}

public sealed class UnlockConnectorResponse
{
    public UnlockStatus Status { get; set; }
}
