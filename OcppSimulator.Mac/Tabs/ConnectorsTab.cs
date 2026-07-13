using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Ocpp.Core.Types;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Live connector state and the actions that drive a charging session.</summary>
public sealed class ConnectorsTab : ChargePointTab
{
    public sealed class ConnectorRow
    {
        public int Id { get; init; }
        public string Status { get; init; } = "";
        public string ErrorCode { get; init; } = "";
        public string Cable { get; init; } = "";
        public string Availability { get; init; } = "";
        public string Meter { get; init; } = "";
        public string Soc { get; init; } = "";
        public string Transaction { get; init; } = "";
    }

    private readonly DataGrid _grid = new()
    {
        IsReadOnly = true,
        AutoGenerateColumns = false,
        CanUserResizeColumns = true,
        GridLinesVisibility = DataGridGridLinesVisibility.All,
    };

    private readonly NumericUpDown _connectorId = new() { Minimum = 1, Maximum = 50, Value = 1, Width = 90 };
    private readonly TextBox _idTag = new() { Text = "RFID-001", Width = 150 };
    private readonly NumericUpDown _power = new() { Minimum = 0, Maximum = 350000, Value = 11000, Increment = 1000, Width = 120 };
    private readonly ComboBox _errorCode = UiHelpers.EnumCombo(ChargePointErrorCode.NoError);

    protected override Control Build()
    {
        _grid.Columns.Add(Col("Konnektör", nameof(ConnectorRow.Id)));
        _grid.Columns.Add(Col("Durum", nameof(ConnectorRow.Status)));
        _grid.Columns.Add(Col("Hata Kodu", nameof(ConnectorRow.ErrorCode)));
        _grid.Columns.Add(Col("Kablo", nameof(ConnectorRow.Cable)));
        _grid.Columns.Add(Col("Kullanılabilirlik", nameof(ConnectorRow.Availability)));
        _grid.Columns.Add(Col("Sayaç (Wh)", nameof(ConnectorRow.Meter)));
        _grid.Columns.Add(Col("SoC %", nameof(ConnectorRow.Soc)));
        _grid.Columns.Add(Col("Transaction", nameof(ConnectorRow.Transaction)));
        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.SelectedItem is ConnectorRow r && r.Id >= 1)
                _connectorId.Value = r.Id;
        };

        var actions = BuildActions();

        var dock = new DockPanel();
        DockPanel.SetDock(actions, Dock.Bottom);
        dock.Children.Add(actions);
        dock.Children.Add(_grid); // fills remaining
        return dock;
    }

    private Control BuildActions()
    {
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 4 };

        panel.Children.Add(Line(UiHelpers.Label("Konnektör"), _connectorId, UiHelpers.Label("idTag"), _idTag));
        panel.Children.Add(Line(
            UiHelpers.Button("Kablo Tak", () => Run(() => Cp.PlugInAsync(Cid))),
            UiHelpers.Button("Kablo Çıkar", () => Run(() => Cp.UnplugAsync(Cid))),
            UiHelpers.Button("Authorize (RFID)", () => Run(() => Cp.AuthorizeAsync(_idTag.Text ?? "")))));
        panel.Children.Add(Line(
            UiHelpers.Button("Transaction Başlat", () => Run(() => Cp.StartTransactionAsync(Cid, _idTag.Text ?? ""))),
            UiHelpers.Button("Transaction Durdur", () => Run(() => Cp.StopTransactionAsync(Cid)))));
        panel.Children.Add(Line(
            UiHelpers.Button("StatusNotification", () => Run(() => Cp.SendStatusNotificationAsync(Cid)), 150),
            UiHelpers.Button("MeterValues Gönder", () => Run(() => Cp.SendMeterValuesAsync(Cid)), 150)));
        panel.Children.Add(Line(
            UiHelpers.Label("Şarj Gücü (W)"), _power,
            UiHelpers.Button("Gücü Uygula", ApplyPower, 120)));
        panel.Children.Add(Line(
            UiHelpers.Label("Hata Kodu"), _errorCode,
            UiHelpers.Button("Arıza Ayarla", () => Run(() => Cp.SetFaultAsync(Cid, _errorCode.SelectedEnum<ChargePointErrorCode>())), 120)));

        return new Border
        {
            BorderBrush = Avalonia.Media.Brushes.LightGray,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = panel,
        };
    }

    private static StackPanel Line(params Control[] controls)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 2, 0, 2) };
        foreach (var c in controls) { c.VerticalAlignment = VerticalAlignment.Center; p.Children.Add(c); }
        return p;
    }

    private int Cid => (int)(_connectorId.Value ?? 1);

    private void ApplyPower()
    {
        var c = Cp.GetConnector(Cid);
        if (c is not null) { c.ChargingPowerW = (double)(_power.Value ?? 11000); Cp.RaiseStateChanged(); }
    }

    public override void RefreshUi()
    {
        var selectedId = (_grid.SelectedItem as ConnectorRow)?.Id;
        var rows = Cp.Connectors.Select(c => new ConnectorRow
        {
            Id = c.Id,
            Status = c.Status.ToString(),
            ErrorCode = c.ErrorCode.ToString(),
            Cable = c.Id == 0 ? "-" : (c.CablePluggedIn ? "Takılı" : "Boş"),
            Availability = c.Availability.ToString(),
            Meter = c.Id == 0 ? "-" : ((int)System.Math.Round(c.MeterWh)).ToString(),
            Soc = c.Id == 0 ? "-" : ((int)System.Math.Round(c.StateOfChargePercent)).ToString(),
            Transaction = c.ActiveTransaction?.TransactionId.ToString() ?? "-",
        }).ToList();

        _grid.ItemsSource = rows;
        if (selectedId is int id)
            _grid.SelectedItem = rows.FirstOrDefault(r => r.Id == id);
    }

    private static DataGridTextColumn Col(string header, string prop) => new()
    {
        Header = header,
        Binding = new Binding(prop),
        Width = new DataGridLength(1, DataGridLengthUnitType.Star),
    };
}
