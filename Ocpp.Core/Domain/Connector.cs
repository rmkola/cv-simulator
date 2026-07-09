using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>
/// State of one connector of the simulated Charge Point. Connector 0 represents the Charge Point
/// as a whole (main controller) and does not hold transactions.
/// </summary>
public sealed class Connector
{
    public int Id { get; }

    public ChargePointStatus Status { get; set; } = ChargePointStatus.Available;
    public ChargePointErrorCode ErrorCode { get; set; } = ChargePointErrorCode.NoError;

    /// <summary>Whether a cable/EV is currently plugged in.</summary>
    public bool CablePluggedIn { get; set; }

    /// <summary>Operative vs Inoperative (ChangeAvailability). Inoperative -> reported Unavailable.</summary>
    public AvailabilityType Availability { get; set; } = AvailabilityType.Operative;

    /// <summary>Requested availability change deferred until the current transaction finishes.</summary>
    public AvailabilityType? PendingAvailability { get; set; }

    /// <summary>Simulated energy register in Wh. Increments while charging.</summary>
    public double MeterWh { get; set; }

    /// <summary>Simulated instantaneous charging power in watts (used to advance the meter).</summary>
    public double ChargingPowerW { get; set; } = 11000;

    /// <summary>Simulated EV state of charge in percent (advances slowly while charging).</summary>
    public double StateOfChargePercent { get; set; } = 20;

    /// <summary>The active transaction on this connector, if any.</summary>
    public Transaction? ActiveTransaction { get; set; }

    /// <summary>Reservation currently held on this connector, if any.</summary>
    public int? ReservationId { get; set; }

    public bool IsCharging => ActiveTransaction is { IsActive: true } && Status == ChargePointStatus.Charging;

    public Connector(int id) => Id = id;
}
