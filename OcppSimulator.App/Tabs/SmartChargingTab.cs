namespace OcppSimulator.App.Tabs;

/// <summary>Installed charging profiles (set via SetChargingProfile, removed via ClearChargingProfile).</summary>
public sealed class SmartChargingTab : ChargePointTab
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };

    public SmartChargingTab()
    {
        _grid.Columns.Add("connector", "Konnektör");
        _grid.Columns.Add("id", "Profil Id");
        _grid.Columns.Add("purpose", "Amaç");
        _grid.Columns.Add("kind", "Tür");
        _grid.Columns.Add("stack", "Stack");
        _grid.Columns.Add("unit", "Birim");
        _grid.Columns.Add("periods", "Periyotlar");
        Controls.Add(_grid);
    }

    public override void RefreshUi()
    {
        _grid.Rows.Clear();
        foreach (var p in Cp.ChargingProfiles.All)
        {
            var sched = p.Profile.ChargingSchedule;
            var periods = string.Join(", ", sched.ChargingSchedulePeriod.Select(x => $"@{x.StartPeriod}s={x.Limit}"));
            _grid.Rows.Add(
                p.ConnectorId,
                p.Profile.ChargingProfileId,
                p.Profile.ChargingProfilePurpose.ToString(),
                p.Profile.ChargingProfileKind.ToString(),
                p.Profile.StackLevel,
                sched.ChargingRateUnit.ToString(),
                periods);
        }
    }
}
