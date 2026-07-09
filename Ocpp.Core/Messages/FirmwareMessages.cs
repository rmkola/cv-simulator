using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Firmware Management profile (spec 6.x) ----

// 6.17 / 6.18 DiagnosticsStatusNotification
public sealed class DiagnosticsStatusNotificationRequest
{
    public DiagnosticsStatus Status { get; set; }
}

public sealed class DiagnosticsStatusNotificationResponse { }

// 6.19 / 6.20 FirmwareStatusNotification
public sealed class FirmwareStatusNotificationRequest
{
    public FirmwareStatus Status { get; set; }
}

public sealed class FirmwareStatusNotificationResponse { }

// 6.25 / 6.26 GetDiagnostics
public sealed class GetDiagnosticsRequest
{
    public string Location { get; set; } = "";
    public int? Retries { get; set; }
    public int? RetryInterval { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? StopTime { get; set; }
}

public sealed class GetDiagnosticsResponse
{
    /// <summary>Optional. Name of the file with diagnostic information that will be uploaded.</summary>
    public string? FileName { get; set; }
}

// 6.55 / 6.56 UpdateFirmware
public sealed class UpdateFirmwareRequest
{
    public string Location { get; set; } = "";
    public int? Retries { get; set; }
    public DateTimeOffset RetrieveDate { get; set; }
    public int? RetryInterval { get; set; }
}

public sealed class UpdateFirmwareResponse { }
