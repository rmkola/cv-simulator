using System.Linq;
using Avalonia.Controls;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Installed charging profiles (SetChargingProfile / ClearChargingProfile).</summary>
public sealed class SmartChargingTab : ChargePointTab
{
    public sealed class ProfileRow
    {
        public int Connector { get; init; }
        public int Id { get; init; }
        public string Purpose { get; init; } = "";
        public string Kind { get; init; } = "";
        public int Stack { get; init; }
        public string Unit { get; init; } = "";
        public string Periods { get; init; } = "";
    }

    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, GridLinesVisibility = DataGridGridLinesVisibility.All };

    protected override Control Build()
    {
        _grid.Columns.Add(LocalAuthListTab.Star("Konnektör", nameof(ProfileRow.Connector)));
        _grid.Columns.Add(LocalAuthListTab.Star("Profil Id", nameof(ProfileRow.Id)));
        _grid.Columns.Add(LocalAuthListTab.Star("Amaç", nameof(ProfileRow.Purpose)));
        _grid.Columns.Add(LocalAuthListTab.Star("Tür", nameof(ProfileRow.Kind)));
        _grid.Columns.Add(LocalAuthListTab.Star("Stack", nameof(ProfileRow.Stack)));
        _grid.Columns.Add(LocalAuthListTab.Star("Birim", nameof(ProfileRow.Unit)));
        _grid.Columns.Add(LocalAuthListTab.Star("Periyotlar", nameof(ProfileRow.Periods)));
        return _grid;
    }

    public override void RefreshUi()
    {
        _grid.ItemsSource = Cp.ChargingProfiles.All.Select(p =>
        {
            var s = p.Profile.ChargingSchedule;
            return new ProfileRow
            {
                Connector = p.ConnectorId,
                Id = p.Profile.ChargingProfileId,
                Purpose = p.Profile.ChargingProfilePurpose.ToString(),
                Kind = p.Profile.ChargingProfileKind.ToString(),
                Stack = p.Profile.StackLevel,
                Unit = s.ChargingRateUnit.ToString(),
                Periods = string.Join(", ", s.ChargingSchedulePeriod.Select(x => $"@{x.StartPeriod}s={x.Limit}")),
            };
        }).ToList();
    }
}
