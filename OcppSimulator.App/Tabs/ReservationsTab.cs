namespace OcppSimulator.App.Tabs;

/// <summary>Active reservations (created by ReserveNow, removed by CancelReservation from the CSMS).</summary>
public sealed class ReservationsTab : ChargePointTab
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };

    public ReservationsTab()
    {
        _grid.Columns.Add("id", "Rezervasyon Id");
        _grid.Columns.Add("connector", "Konnektör");
        _grid.Columns.Add("idTag", "idTag");
        _grid.Columns.Add("parent", "Parent idTag");
        _grid.Columns.Add("expiry", "Bitiş");
        Controls.Add(_grid);
    }

    public override void RefreshUi()
    {
        _grid.Rows.Clear();
        foreach (var r in Cp.Reservations.All.OrderBy(r => r.ReservationId))
            _grid.Rows.Add(r.ReservationId, r.ConnectorId, r.IdTag, r.ParentIdTag ?? "-", r.ExpiryDate.ToString("u"));
    }
}
