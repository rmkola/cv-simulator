using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Read-only view of the Local Authorization List (populated by SendLocalList from the CSMS).</summary>
public sealed class LocalAuthListTab : ChargePointTab
{
    public sealed class AuthRow
    {
        public string IdTag { get; init; } = "";
        public string Status { get; init; } = "";
        public string Expiry { get; init; } = "";
        public string Parent { get; init; } = "";
    }

    private readonly TextBlock _version = new() { FontWeight = FontWeight.Bold, Margin = new Thickness(10, 6, 10, 6) };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, GridLinesVisibility = DataGridGridLinesVisibility.All };

    protected override Control Build()
    {
        _grid.Columns.Add(Star("idTag", nameof(AuthRow.IdTag)));
        _grid.Columns.Add(Star("Durum", nameof(AuthRow.Status)));
        _grid.Columns.Add(Star("Son Kullanım", nameof(AuthRow.Expiry)));
        _grid.Columns.Add(Star("Parent idTag", nameof(AuthRow.Parent)));

        var dock = new DockPanel();
        DockPanel.SetDock(_version, Dock.Top);
        dock.Children.Add(_version);
        dock.Children.Add(_grid);
        return dock;
    }

    public override void RefreshUi()
    {
        _version.Text = $"Liste Versiyonu: {Cp.LocalAuthList.Version}   ·   Kayıt sayısı: {Cp.LocalAuthList.Entries.Count}";
        _grid.ItemsSource = Cp.LocalAuthList.Entries
            .Select(kv => new AuthRow
            {
                IdTag = kv.Key,
                Status = kv.Value.Status.ToString(),
                Expiry = kv.Value.ExpiryDate?.ToString("u") ?? "-",
                Parent = kv.Value.ParentIdTag ?? "-",
            }).ToList();
    }

    internal static DataGridTextColumn Star(string header, string prop) => new()
    {
        Header = header, Binding = new Binding(prop), Width = new DataGridLength(1, DataGridLengthUnitType.Star),
    };
}
