namespace Ocpp.Core.Domain;

public sealed class Reservation
{
    public int ReservationId { get; init; }
    public int ConnectorId { get; init; }
    public string IdTag { get; init; } = "";
    public string? ParentIdTag { get; init; }
    public DateTimeOffset ExpiryDate { get; init; }
}

/// <summary>Active reservations held by the Charge Point (Reservation profile).</summary>
public sealed class ReservationStore
{
    private readonly Dictionary<int, Reservation> _byId = new();

    public IReadOnlyCollection<Reservation> All => _byId.Values;

    public bool TryGet(int reservationId, out Reservation reservation) => _byId.TryGetValue(reservationId, out reservation!);

    public Reservation? ForConnector(int connectorId) =>
        _byId.Values.FirstOrDefault(r => r.ConnectorId == connectorId);

    public void AddOrReplace(Reservation reservation) => _byId[reservation.ReservationId] = reservation;

    public bool Remove(int reservationId) => _byId.Remove(reservationId);
}
