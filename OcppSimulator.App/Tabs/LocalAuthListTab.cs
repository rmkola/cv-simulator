namespace OcppSimulator.App.Tabs;

/// <summary>Read-only view of the Local Authorization List (populated by SendLocalList from the CSMS).</summary>
public sealed class LocalAuthListTab : ChargePointTab
{
    private readonly Label _version = new() { Dock = DockStyle.Top, Height = 28, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Padding = new Padding(6, 4, 0, 0) };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };

    public LocalAuthListTab()
    {
        _grid.Columns.Add("idTag", "idTag");
        _grid.Columns.Add("status", "Durum");
        _grid.Columns.Add("expiry", "Son Kullanım");
        _grid.Columns.Add("parent", "Parent idTag");
        Controls.Add(_grid);
        Controls.Add(_version);
    }

    public override void RefreshUi()
    {
        _version.Text = $"Liste Versiyonu: {Cp.LocalAuthList.Version}   ·   Kayıt sayısı: {Cp.LocalAuthList.Entries.Count}";
        _grid.Rows.Clear();
        foreach (var (idTag, info) in Cp.LocalAuthList.Entries)
            _grid.Rows.Add(idTag, info.Status.ToString(), info.ExpiryDate?.ToString("u") ?? "-", info.ParentIdTag ?? "-");
    }
}
