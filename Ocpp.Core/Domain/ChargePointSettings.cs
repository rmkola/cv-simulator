namespace Ocpp.Core.Domain;

/// <summary>
/// User-editable configuration of the simulated Charge Point: how to reach the Central System,
/// the identity reported in BootNotification, and simulator behavior flags. Serialized to disk
/// so a session can be restored.
/// </summary>
public sealed class ChargePointSettings
{
    // --- Connection ---
    /// <summary>Base Central System URL. The ChargePointId is appended as the last path segment.</summary>
    public string CentralSystemUrl { get; set; } = "ws://localhost:9220/";
    public string ChargePointId { get; set; } = "CP001";
    public string? BasicAuthUser { get; set; }
    public string? BasicAuthPassword { get; set; }

    // --- BootNotification identity (spec 6.3) ---
    public string ChargePointVendor { get; set; } = "CW Enerji";
    public string ChargePointModel { get; set; } = "CW-SIM-1";
    public string? ChargePointSerialNumber { get; set; } = "SIM-0001";
    public string? ChargeBoxSerialNumber { get; set; }
    public string? FirmwareVersion { get; set; } = "1.0.0";
    public string? Iccid { get; set; }
    public string? Imsi { get; set; }
    public string? MeterType { get; set; }
    public string? MeterSerialNumber { get; set; }

    /// <summary>Number of physical connectors (excludes connector 0).</summary>
    public int NumberOfConnectors { get; set; } = 2;

    // --- Behavior ---
    /// <summary>Send BootNotification automatically right after connecting.</summary>
    public bool AutoBootOnConnect { get; set; } = true;

    /// <summary>Periodically send MeterValues for charging connectors.</summary>
    public bool AutoMeterValues { get; set; } = true;

    /// <summary>Default simulated charging power per connector, in watts.</summary>
    public double DefaultChargingPowerW { get; set; } = 11000;

    /// <summary>Builds the connection URI: base URL + ChargePointId as the final path segment.</summary>
    public Uri BuildUri()
    {
        var baseUrl = CentralSystemUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{Uri.EscapeDataString(ChargePointId)}");
    }
}
