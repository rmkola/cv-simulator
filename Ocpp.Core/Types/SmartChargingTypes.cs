namespace Ocpp.Core.Types;

/// <summary>7.14 ChargingSchedulePeriod — maximum power/current usage over a period.</summary>
public sealed class ChargingSchedulePeriod
{
    /// <summary>Required. Start of the period, in seconds from the start of schedule.</summary>
    public int StartPeriod { get; set; }

    /// <summary>Required. Power/current limit during the schedule period (unit = chargingRateUnit).</summary>
    public decimal Limit { get; set; }

    /// <summary>Optional. Number of phases that can be used for charging. Defaults to 3.</summary>
    public int? NumberPhases { get; set; }
}

/// <summary>7.13 ChargingSchedule.</summary>
public sealed class ChargingSchedule
{
    /// <summary>Optional. Duration of the charging schedule in seconds.</summary>
    public int? Duration { get; set; }

    /// <summary>Optional. Starting point of an absolute schedule.</summary>
    public DateTimeOffset? StartSchedule { get; set; }

    /// <summary>Required. The unit of measure Limit is expressed in.</summary>
    public ChargingRateUnitType ChargingRateUnit { get; set; }

    /// <summary>Required. List of ChargingSchedulePeriod elements.</summary>
    public List<ChargingSchedulePeriod> ChargingSchedulePeriod { get; set; } = new();

    /// <summary>Optional. Minimum charging rate supported by the EV.</summary>
    public decimal? MinChargingRate { get; set; }
}

/// <summary>7.8 ChargingProfile.</summary>
public sealed class ChargingProfile
{
    /// <summary>Required. Unique identifier for this profile.</summary>
    public int ChargingProfileId { get; set; }

    /// <summary>Optional. Only valid for TxProfile: the transaction this profile applies to.</summary>
    public int? TransactionId { get; set; }

    /// <summary>Required. Priority; higher levels override lower ones at the same purpose.</summary>
    public int StackLevel { get; set; }

    /// <summary>Required. Defines the purpose of the schedule transferred by this profile.</summary>
    public ChargingProfilePurposeType ChargingProfilePurpose { get; set; }

    /// <summary>Required. Indicates the kind of schedule.</summary>
    public ChargingProfileKindType ChargingProfileKind { get; set; }

    /// <summary>Optional. Indicates the start point of a recurrence.</summary>
    public RecurrencyKindType? RecurrencyKind { get; set; }

    /// <summary>Optional. Point in time at which the profile starts to be valid.</summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>Optional. Point in time at which the profile stops being valid.</summary>
    public DateTimeOffset? ValidTo { get; set; }

    /// <summary>Required. Contains limits for the available power or current over time.</summary>
    public ChargingSchedule ChargingSchedule { get; set; } = new();
}
