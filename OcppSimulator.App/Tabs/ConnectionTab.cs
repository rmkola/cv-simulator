using Ocpp.Core.Domain;

namespace OcppSimulator.App.Tabs;

/// <summary>Connection settings + BootNotification identity. Connect / disconnect / boot actions.</summary>
public sealed class ConnectionTab : ChargePointTab
{
    private readonly TextBox _url = UiHelpers.MakeTextBox();
    private readonly TextBox _cpId = UiHelpers.MakeTextBox();
    private readonly TextBox _authUser = UiHelpers.MakeTextBox();
    private readonly TextBox _authPass = UiHelpers.MakeTextBox();
    private readonly TextBox _vendor = UiHelpers.MakeTextBox();
    private readonly TextBox _model = UiHelpers.MakeTextBox();
    private readonly TextBox _serial = UiHelpers.MakeTextBox();
    private readonly TextBox _chargeBoxSerial = UiHelpers.MakeTextBox();
    private readonly TextBox _firmware = UiHelpers.MakeTextBox();
    private readonly TextBox _iccid = UiHelpers.MakeTextBox();
    private readonly TextBox _imsi = UiHelpers.MakeTextBox();
    private readonly TextBox _meterType = UiHelpers.MakeTextBox();
    private readonly TextBox _meterSerial = UiHelpers.MakeTextBox();
    private readonly NumericUpDown _connectors = new() { Minimum = 1, Maximum = 50, Value = 2, Width = 80 };
    private readonly CheckBox _autoBoot = new() { Text = "Bağlanınca otomatik BootNotification", AutoSize = true };
    private readonly CheckBox _autoMeter = new() { Text = "Otomatik MeterValues", AutoSize = true };

    private readonly Button _connectBtn;
    private readonly Button _disconnectBtn;
    private readonly Button _bootBtn;

    public ConnectionTab()
    {
        _connectBtn = UiHelpers.MakeButton("Bağlan", (_, _) => OnConnect());
        _disconnectBtn = UiHelpers.MakeButton("Bağlantıyı Kes", (_, _) => Run(() => Cp.DisconnectAsync()));
        _bootBtn = UiHelpers.MakeButton("BootNotification Gönder", (_, _) => Run(() => Cp.SendBootNotificationAsync()));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSection(layout, "Bağlantı");
        AddRow(layout, "Central System URL", _url);
        AddRow(layout, "Charge Point ID", _cpId);
        AddRow(layout, "Basic Auth Kullanıcı", _authUser);
        AddRow(layout, "Basic Auth Şifre", _authPass);

        AddSection(layout, "BootNotification Kimliği");
        AddRow(layout, "Vendor", _vendor);
        AddRow(layout, "Model", _model);
        AddRow(layout, "Seri No", _serial);
        AddRow(layout, "ChargeBox Seri No", _chargeBoxSerial);
        AddRow(layout, "Firmware Sürümü", _firmware);
        AddRow(layout, "ICCID", _iccid);
        AddRow(layout, "IMSI", _imsi);
        AddRow(layout, "Meter Type", _meterType);
        AddRow(layout, "Meter Seri No", _meterSerial);

        AddSection(layout, "Davranış");
        AddRow(layout, "Konnektör Sayısı", _connectors);
        AddRow(layout, "", _autoBoot);
        AddRow(layout, "", _autoMeter);

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.AddRange(new Control[] { _connectBtn, _disconnectBtn, _bootBtn });
        AddRow(layout, "", buttons);

        Controls.Add(layout);
    }

    protected override void OnBound() => LoadFromSettings();

    private void LoadFromSettings()
    {
        var s = Cp.Settings;
        _url.Text = s.CentralSystemUrl;
        _cpId.Text = s.ChargePointId;
        _authUser.Text = s.BasicAuthUser ?? "";
        _authPass.Text = s.BasicAuthPassword ?? "";
        _vendor.Text = s.ChargePointVendor;
        _model.Text = s.ChargePointModel;
        _serial.Text = s.ChargePointSerialNumber ?? "";
        _chargeBoxSerial.Text = s.ChargeBoxSerialNumber ?? "";
        _firmware.Text = s.FirmwareVersion ?? "";
        _iccid.Text = s.Iccid ?? "";
        _imsi.Text = s.Imsi ?? "";
        _meterType.Text = s.MeterType ?? "";
        _meterSerial.Text = s.MeterSerialNumber ?? "";
        _connectors.Value = Math.Clamp(s.NumberOfConnectors, 1, 50);
        _autoBoot.Checked = s.AutoBootOnConnect;
        _autoMeter.Checked = s.AutoMeterValues;
    }

    private void ApplyToSettings()
    {
        var s = Cp.Settings;
        s.CentralSystemUrl = _url.Text.Trim();
        s.ChargePointId = _cpId.Text.Trim();
        s.BasicAuthUser = Empty(_authUser.Text);
        s.BasicAuthPassword = Empty(_authPass.Text);
        s.ChargePointVendor = _vendor.Text.Trim();
        s.ChargePointModel = _model.Text.Trim();
        s.ChargePointSerialNumber = Empty(_serial.Text);
        s.ChargeBoxSerialNumber = Empty(_chargeBoxSerial.Text);
        s.FirmwareVersion = Empty(_firmware.Text);
        s.Iccid = Empty(_iccid.Text);
        s.Imsi = Empty(_imsi.Text);
        s.MeterType = Empty(_meterType.Text);
        s.MeterSerialNumber = Empty(_meterSerial.Text);

        var newCount = (int)_connectors.Value;
        if (newCount != s.NumberOfConnectors)
        {
            s.NumberOfConnectors = newCount;
            Cp.RebuildConnectors();
        }
        s.AutoBootOnConnect = _autoBoot.Checked;
        s.AutoMeterValues = _autoMeter.Checked;
        SettingsStore.Save(s);
    }

    private static string? Empty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void OnConnect()
    {
        ApplyToSettings();
        Run(() => Cp.ConnectAsync());
    }

    public override void RefreshUi()
    {
        var connected = Cp.IsConnected;
        _connectBtn.Enabled = !connected;
        _disconnectBtn.Enabled = connected;
        _bootBtn.Enabled = connected;
    }

    private static void AddSection(TableLayoutPanel layout, string title)
    {
        var lbl = new Label { Text = title, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), AutoSize = true, Margin = new Padding(3, 12, 3, 4) };
        int row = layout.RowCount++;
        layout.Controls.Add(lbl, 0, row);
        layout.SetColumnSpan(lbl, 2);
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control)
    {
        int row = layout.RowCount++;
        layout.Controls.Add(UiHelpers.MakeLabel(label), 0, row);
        layout.Controls.Add(control, 1, row);
    }
}
