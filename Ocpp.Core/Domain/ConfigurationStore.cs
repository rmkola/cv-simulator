using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>A single standard configuration key (spec section 9).</summary>
public sealed class ConfigurationItem
{
    public string Key { get; }
    public bool Readonly { get; }
    public string Value { get; set; }

    public ConfigurationItem(string key, string value, bool @readonly)
    {
        Key = key;
        Value = value;
        Readonly = @readonly;
    }

    public KeyValue ToKeyValue() => new() { Key = Key, Readonly = Readonly, Value = Value };
}

/// <summary>
/// Holds the 43 standard OCPP 1.6 configuration keys (spec section 9) with defaults and RO/RW
/// flags. Backs the GetConfiguration / ChangeConfiguration operations.
/// </summary>
public sealed class ConfigurationStore
{
    private readonly Dictionary<string, ConfigurationItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _order = new();

    public ConfigurationStore()
    {
        // Core profile (9.1)
        Add("AllowOfflineTxForUnknownId", "false", false);
        Add("AuthorizationCacheEnabled", "false", false);
        Add("AuthorizeRemoteTxRequests", "false", false);
        Add("BlinkRepeat", "0", false);
        Add("ClockAlignedDataInterval", "0", false);
        Add("ConnectionTimeOut", "60", false);
        Add("GetConfigurationMaxKeys", "100", true);
        Add("HeartbeatInterval", "300", false);
        Add("LightIntensity", "100", false);
        Add("LocalAuthorizeOffline", "true", false);
        Add("LocalPreAuthorize", "false", false);
        Add("MaxEnergyOnInvalidId", "0", false);
        Add("MeterValuesAlignedData", "Energy.Active.Import.Register", false);
        Add("MeterValuesAlignedDataMaxLength", "4", true);
        Add("MeterValuesSampledData", "Energy.Active.Import.Register", false);
        Add("MeterValuesSampledDataMaxLength", "4", true);
        Add("MeterValueSampleInterval", "60", false);
        Add("MinimumStatusDuration", "0", false);
        Add("NumberOfConnectors", "2", true);
        Add("ResetRetries", "3", false);
        Add("ConnectorPhaseRotation", "NotApplicable", false);
        Add("ConnectorPhaseRotationMaxLength", "2", true);
        Add("StopTransactionOnEVSideDisconnect", "true", false);
        Add("StopTransactionOnInvalidId", "true", false);
        Add("StopTxnAlignedData", "", false);
        Add("StopTxnAlignedDataMaxLength", "4", true);
        Add("StopTxnSampledData", "", false);
        Add("StopTxnSampledDataMaxLength", "4", true);
        Add("SupportedFeatureProfiles",
            "Core,FirmwareManagement,LocalAuthListManagement,Reservation,SmartCharging,RemoteTrigger", true);
        Add("SupportedFeatureProfilesMaxLength", "6", true);
        Add("TransactionMessageAttempts", "3", false);
        Add("TransactionMessageRetryInterval", "60", false);
        Add("UnlockConnectorOnEVSideDisconnect", "true", false);
        Add("WebSocketPingInterval", "60", false);

        // Local Auth List Management profile (9.2)
        Add("LocalAuthListEnabled", "true", false);
        Add("LocalAuthListMaxLength", "1000", true);
        Add("SendLocalListMaxLength", "100", true);

        // Reservation profile (9.3)
        Add("ReserveConnectorZeroSupported", "false", true);

        // Smart Charging profile (9.4)
        Add("ChargeProfileMaxStackLevel", "10", true);
        Add("ChargingScheduleAllowedChargingRateUnit", "Current,Power", true);
        Add("ChargingScheduleMaxPeriods", "100", true);
        Add("ConnectorSwitch3to1PhaseSupported", "false", true);
        Add("MaxChargingProfilesInstalled", "10", true);
    }

    private void Add(string key, string value, bool ro)
    {
        _items[key] = new ConfigurationItem(key, value, ro);
        _order.Add(key);
    }

    public IReadOnlyList<ConfigurationItem> All => _order.Select(k => _items[k]).ToList();

    public bool TryGet(string key, out ConfigurationItem item) => _items.TryGetValue(key, out item!);

    public string? GetValue(string key) => _items.TryGetValue(key, out var i) ? i.Value : null;

    public int GetInt(string key, int fallback)
        => int.TryParse(GetValue(key), out var v) ? v : fallback;

    public bool GetBool(string key, bool fallback)
        => bool.TryParse(GetValue(key), out var v) ? v : fallback;

    /// <summary>Applies a ChangeConfiguration request. Returns the spec status.</summary>
    public ConfigurationStatus Set(string key, string value)
    {
        if (!_items.TryGetValue(key, out var item))
            return ConfigurationStatus.NotSupported;
        if (item.Readonly)
            return ConfigurationStatus.Rejected;

        item.Value = value;
        return ConfigurationStatus.Accepted;
    }

    /// <summary>Builds a GetConfiguration response for the (optionally filtered) keys.</summary>
    public (List<KeyValue> known, List<string> unknown) Query(IReadOnlyCollection<string>? keys)
    {
        if (keys is null || keys.Count == 0)
            return (All.Select(i => i.ToKeyValue()).ToList(), new List<string>());

        var known = new List<KeyValue>();
        var unknown = new List<string>();
        foreach (var k in keys)
        {
            if (_items.TryGetValue(k, out var item)) known.Add(item.ToKeyValue());
            else unknown.Add(k);
        }
        return (known, unknown);
    }
}
