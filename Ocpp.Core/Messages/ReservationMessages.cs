using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Reservation profile (spec 6.x) ----

// 6.5 / 6.6 CancelReservation
public sealed class CancelReservationRequest
{
    public int ReservationId { get; set; }
}

public sealed class CancelReservationResponse
{
    public CancelReservationStatus Status { get; set; }
}

// 6.37 / 6.38 ReserveNow
public sealed class ReserveNowRequest
{
    public int ConnectorId { get; set; }
    public DateTimeOffset ExpiryDate { get; set; }
    public string IdTag { get; set; } = "";
    public string? ParentIdTag { get; set; }
    public int ReservationId { get; set; }
}

public sealed class ReserveNowResponse
{
    public ReservationStatus Status { get; set; }
}
