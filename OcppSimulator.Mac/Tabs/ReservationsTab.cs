using System.Linq;
using Avalonia.Controls;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Active reservations (ReserveNow / CancelReservation from the CSMS).</summary>
public sealed class ReservationsTab : ChargePointTab
{
    public sealed class ResRow
    {
        public int Id { get; init; }
        public int Connector { get; init; }
        public string IdTag { get; init; } = "";
        public string Parent { get; init; } = "";
        public string Expiry { get; init; } = "";
    }

    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, GridLinesVisibility = DataGridGridLinesVisibility.All };

    protected override Control Build()
    {
        _grid.Columns.Add(LocalAuthListTab.Star("Rezervasyon Id", nameof(ResRow.Id)));
        _grid.Columns.Add(LocalAuthListTab.Star("Konnektör", nameof(ResRow.Connector)));
        _grid.Columns.Add(LocalAuthListTab.Star("idTag", nameof(ResRow.IdTag)));
        _grid.Columns.Add(LocalAuthListTab.Star("Parent idTag", nameof(ResRow.Parent)));
        _grid.Columns.Add(LocalAuthListTab.Star("Bitiş", nameof(ResRow.Expiry)));
        return _grid;
    }

    public override void RefreshUi()
    {
        _grid.ItemsSource = Cp.Reservations.All.OrderBy(r => r.ReservationId)
            .Select(r => new ResRow
            {
                Id = r.ReservationId,
                Connector = r.ConnectorId,
                IdTag = r.IdTag,
                Parent = r.ParentIdTag ?? "-",
                Expiry = r.ExpiryDate.ToString("u"),
            }).ToList();
    }
}
