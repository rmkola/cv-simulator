using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Smart Charging profile (spec 6.x) ----

// 6.13 / 6.14 ClearChargingProfile
public sealed class ClearChargingProfileRequest
{
    public int? Id { get; set; }
    public int? ConnectorId { get; set; }
    public ChargingProfilePurposeType? ChargingProfilePurpose { get; set; }
    public int? StackLevel { get; set; }
}

public sealed class ClearChargingProfileResponse
{
    public ClearChargingProfileStatus Status { get; set; }
}

// 6.21 / 6.22 GetCompositeSchedule
public sealed class GetCompositeScheduleRequest
{
    public int ConnectorId { get; set; }
    public int Duration { get; set; }
    public ChargingRateUnitType? ChargingRateUnit { get; set; }
}

public sealed class GetCompositeScheduleResponse
{
    public GetCompositeScheduleStatus Status { get; set; }
    public int? ConnectorId { get; set; }
    public DateTimeOffset? ScheduleStart { get; set; }
    public ChargingSchedule? ChargingSchedule { get; set; }
}

// 6.43 / 6.44 SetChargingProfile
public sealed class SetChargingProfileRequest
{
    public int ConnectorId { get; set; }
    public ChargingProfile CsChargingProfiles { get; set; } = new();
}

public sealed class SetChargingProfileResponse
{
    public ChargingProfileStatus Status { get; set; }
}
