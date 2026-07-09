using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>An installed charging profile together with the connector it was set on.</summary>
public sealed class InstalledChargingProfile
{
    public int ConnectorId { get; init; }
    public ChargingProfile Profile { get; init; } = new();
}

/// <summary>
/// Stores charging profiles installed via SetChargingProfile (Smart Charging profile). A new
/// profile replaces an existing one with the same chargingProfileId, or the same
/// connector + stackLevel + purpose combination (spec 5.16.3 / 5.16.4).
/// </summary>
public sealed class ChargingProfileStore
{
    private readonly List<InstalledChargingProfile> _profiles = new();

    public IReadOnlyList<InstalledChargingProfile> All => _profiles;

    public void Set(int connectorId, ChargingProfile profile)
    {
        _profiles.RemoveAll(p =>
            p.Profile.ChargingProfileId == profile.ChargingProfileId ||
            (p.ConnectorId == connectorId &&
             p.Profile.StackLevel == profile.StackLevel &&
             p.Profile.ChargingProfilePurpose == profile.ChargingProfilePurpose));

        _profiles.Add(new InstalledChargingProfile { ConnectorId = connectorId, Profile = profile });
    }

    /// <summary>
    /// Clears profiles matching the ClearChargingProfile criteria. Any criterion left null matches
    /// all. Returns true if at least one profile was removed.
    /// </summary>
    public bool Clear(int? id, int? connectorId, ChargingProfilePurposeType? purpose, int? stackLevel)
    {
        int removed = _profiles.RemoveAll(p =>
            (id is null || p.Profile.ChargingProfileId == id) &&
            (connectorId is null || p.ConnectorId == connectorId) &&
            (purpose is null || p.Profile.ChargingProfilePurpose == purpose) &&
            (stackLevel is null || p.Profile.StackLevel == stackLevel));
        return removed > 0;
    }
}
