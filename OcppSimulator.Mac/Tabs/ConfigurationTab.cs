using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Ocpp.Core.Types;

namespace OcppSimulator.Mac.Tabs;

/// <summary>The 43 standard configuration keys. RW values are editable in place.</summary>
public sealed class ConfigurationTab : ChargePointTab
{
    public sealed class ConfigRow
    {
        public string Key { get; init; } = "";
        public string Value { get; set; } = "";
        public bool Readonly { get; init; }
    }

    private readonly DataGrid _grid = new()
    {
        AutoGenerateColumns = false,
        GridLinesVisibility = DataGridGridLinesVisibility.All,
        CanUserResizeColumns = true,
    };

    private string? _notice;
    private readonly TextBlock _info = new()
    {
        Foreground = Brushes.DimGray, Margin = new Thickness(10, 6, 10, 6),
        Text = "Değeri değiştirmek CSMS'ten gelen ChangeConfiguration ile aynı etkiyi yapar. Salt okunur anahtarlar reddedilir.",
    };

    protected override Control Build()
    {
        _grid.Columns.Add(new DataGridTextColumn { Header = "Anahtar", Binding = new Binding(nameof(ConfigRow.Key)), IsReadOnly = true, Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Değer", Binding = new Binding(nameof(ConfigRow.Value)) { Mode = BindingMode.TwoWay }, Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Salt Okunur", Binding = new Binding(nameof(ConfigRow.Readonly)), IsReadOnly = true, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

        _grid.CellEditEnded += (_, e) =>
        {
            if (e.Row.DataContext is not ConfigRow row) return;
            var status = Cp.Configuration.Set(row.Key, row.Value);
            if (status != ConfigurationStatus.Accepted)
            {
                _notice = $"{row.Key}: {status}";
                RefreshUi(); // revert to stored value
            }
        };

        var dock = new DockPanel();
        DockPanel.SetDock(_info, Dock.Top);
        dock.Children.Add(_info);
        dock.Children.Add(_grid);
        return dock;
    }

    public override void RefreshUi()
    {
        _grid.ItemsSource = Cp.Configuration.All
            .Select(i => new ConfigRow { Key = i.Key, Value = i.Value, Readonly = i.Readonly })
            .ToList();

        _info.Text = _notice is null
            ? "Değeri değiştirmek CSMS'ten gelen ChangeConfiguration ile aynı etkiyi yapar. Salt okunur anahtarlar reddedilir."
            : $"⚠ {_notice}";
        _notice = null;
    }
}
