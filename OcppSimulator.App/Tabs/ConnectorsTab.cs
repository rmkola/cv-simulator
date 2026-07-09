using Ocpp.Core.Types;

namespace OcppSimulator.App.Tabs;

/// <summary>Live connector state and the actions that drive a charging session.</summary>
public sealed class ConnectorsTab : ChargePointTab
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
    };

    private readonly NumericUpDown _connectorId = new() { Minimum = 1, Maximum = 50, Value = 1, Width = 60 };
    private readonly TextBox _idTag = new() { Text = "RFID-001", Width = 140 };
    private readonly NumericUpDown _power = new() { Minimum = 0, Maximum = 350000, Value = 11000, Increment = 1000, Width = 90 };
    private readonly ComboBox _errorCode = UiHelpers.MakeEnumCombo(ChargePointErrorCode.NoError);

    public ConnectorsTab()
    {
        _grid.Columns.Add("id", "Konnektör");
        _grid.Columns.Add("status", "Durum");
        _grid.Columns.Add("error", "Hata Kodu");
        _grid.Columns.Add("plugged", "Kablo");
        _grid.Columns.Add("avail", "Kullanılabilirlik");
        _grid.Columns.Add("meter", "Sayaç (Wh)");
        _grid.Columns.Add("soc", "SoC %");
        _grid.Columns.Add("tx", "Transaction");
        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.CurrentRow?.Cells["id"].Value is int id && id >= 1)
                _connectorId.Value = Math.Clamp(id, (int)_connectorId.Minimum, (int)_connectorId.Maximum);
        };

        var actions = BuildActionPanel();

        // Grid stretches to fill; the action panel auto-sizes to its content at the bottom, so its
        // last rows (power / fault) are always fully visible and never hidden under the window footer.
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_grid, 0, 0);
        layout.Controls.Add(actions, 0, 1);
        Controls.Add(layout);
    }

    private Control BuildActionPanel()
    {
        // A TableLayoutPanel with AutoSize rows sizes deterministically, so every action row
        // (including the last "Hata Kodu" row) is always fully visible above the window footer.
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Padding = new Padding(8, 6, 8, 6),
        };

        AddRow(table, Row(
            UiHelpers.MakeLabel("Konnektör"), _connectorId, UiHelpers.MakeLabel("idTag"), _idTag));
        AddRow(table, Row(
            UiHelpers.MakeButton("Kablo Tak", (_, _) => Run(() => Cp.PlugInAsync(Cid))),
            UiHelpers.MakeButton("Kablo Çıkar", (_, _) => Run(() => Cp.UnplugAsync(Cid))),
            UiHelpers.MakeButton("Authorize (RFID)", (_, _) => Run(() => Cp.AuthorizeAsync(_idTag.Text)))));
        AddRow(table, Row(
            UiHelpers.MakeButton("Transaction Başlat", (_, _) => Run(() => Cp.StartTransactionAsync(Cid, _idTag.Text))),
            UiHelpers.MakeButton("Transaction Durdur", (_, _) => Run(() => Cp.StopTransactionAsync(Cid)))));
        AddRow(table, Row(
            UiHelpers.MakeButton("StatusNotification", (_, _) => Run(() => Cp.SendStatusNotificationAsync(Cid)), 140),
            UiHelpers.MakeButton("MeterValues Gönder", (_, _) => Run(() => Cp.SendMeterValuesAsync(Cid)), 140)));
        AddRow(table, Row(
            UiHelpers.MakeLabel("Şarj Gücü (W)"), _power,
            UiHelpers.MakeButton("Gücü Uygula", (_, _) => ApplyPower(), 110)));
        AddRow(table, Row(
            UiHelpers.MakeLabel("Hata Kodu"), _errorCode,
            UiHelpers.MakeButton("Arıza Ayarla", (_, _) => Run(() => Cp.SetFaultAsync(Cid, _errorCode.SelectedEnum<ChargePointErrorCode>())), 110)));

        return table;
    }

    private static void AddRow(TableLayoutPanel table, Control row)
    {
        int r = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(row, 0, r);
    }

    /// <summary>One horizontal action row that sizes to its controls.</summary>
    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0),
        };
        row.Controls.AddRange(controls);
        return row;
    }

    private int Cid => (int)_connectorId.Value;

    private void ApplyPower()
    {
        var c = Cp.GetConnector(Cid);
        if (c is not null) { c.ChargingPowerW = (double)_power.Value; Cp.RaiseStateChanged(); }
    }

    public override void RefreshUi()
    {
        var selected = _grid.CurrentRow?.Cells["id"].Value as int?;
        _grid.Rows.Clear();
        foreach (var c in Cp.Connectors)
        {
            _grid.Rows.Add(
                c.Id,
                c.Status.ToString(),
                c.ErrorCode.ToString(),
                c.Id == 0 ? "-" : (c.CablePluggedIn ? "Takılı" : "Boş"),
                c.Availability.ToString(),
                c.Id == 0 ? "-" : ((int)Math.Round(c.MeterWh)).ToString(),
                c.Id == 0 ? "-" : ((int)Math.Round(c.StateOfChargePercent)).ToString(),
                c.ActiveTransaction?.TransactionId.ToString() ?? "-");
        }

        foreach (DataGridViewRow row in _grid.Rows)
        {
            var status = row.Cells["status"].Value?.ToString();
            row.Cells["status"].Style.BackColor = status switch
            {
                nameof(ChargePointStatus.Charging) => Color.FromArgb(200, 240, 200),
                nameof(ChargePointStatus.Faulted) => Color.FromArgb(245, 200, 200),
                nameof(ChargePointStatus.Available) => Color.FromArgb(225, 240, 255),
                nameof(ChargePointStatus.Reserved) => Color.FromArgb(255, 240, 200),
                _ => Color.White,
            };
            if (selected is int s && row.Cells["id"].Value is int rid && rid == s) row.Selected = true;
        }
    }
}
