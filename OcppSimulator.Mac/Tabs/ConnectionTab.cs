using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Connection settings + BootNotification identity. Connect / disconnect / boot.</summary>
public sealed class ConnectionTab : ChargePointTab
{
    private readonly TextBox _url = UiHelpers.TextBox();
    private readonly TextBox _cpId = UiHelpers.TextBox();
    private readonly TextBox _authUser = UiHelpers.TextBox();
    private readonly TextBox _authPass = UiHelpers.TextBox();
    private readonly TextBox _vendor = UiHelpers.TextBox();
    private readonly TextBox _model = UiHelpers.TextBox();
    private readonly TextBox _serial = UiHelpers.TextBox();
    private readonly TextBox _chargeBoxSerial = UiHelpers.TextBox();
    private readonly TextBox _firmware = UiHelpers.TextBox();
    private readonly TextBox _iccid = UiHelpers.TextBox();
    private readonly TextBox _imsi = UiHelpers.TextBox();
    private readonly TextBox _meterType = UiHelpers.TextBox();
    private readonly TextBox _meterSerial = UiHelpers.TextBox();
    private readonly NumericUpDown _connectors = new() { Minimum = 1, Maximum = 50, Value = 2, Width = 100, Margin = new Thickness(0, 3, 6, 3) };
    private readonly CheckBox _autoBoot = new() { Content = "Bağlanınca otomatik BootNotification" };
    private readonly CheckBox _autoMeter = new() { Content = "Otomatik MeterValues" };

    private readonly Button _connectBtn;
    private readonly Button _disconnectBtn;
    private readonly Button _bootBtn;

    public ConnectionTab()
    {
        _connectBtn = UiHelpers.Button("Bağlan", OnConnect);
        _disconnectBtn = UiHelpers.Button("Bağlantıyı Kes", () => Run(() => Cp.DisconnectAsync()));
        _bootBtn = UiHelpers.Button("BootNotification Gönder", () => Run(() => Cp.SendBootNotificationAsync()));
    }

    protected override Control Build()
    {
        var stack = new StackPanel { Margin = new Thickness(14), Spacing = 2 };

        stack.Children.Add(Section("Bağlantı"));
        stack.Children.Add(Row("Central System URL", _url));
        stack.Children.Add(Row("Charge Point ID", _cpId));
        stack.Children.Add(Row("Basic Auth Kullanıcı", _authUser));
        stack.Children.Add(Row("Basic Auth Şifre", _authPass));

        stack.Children.Add(Section("BootNotification Kimliği"));
        stack.Children.Add(Row("Vendor", _vendor));
        stack.Children.Add(Row("Model", _model));
        stack.Children.Add(Row("Seri No", _serial));
        stack.Children.Add(Row("ChargeBox Seri No", _chargeBoxSerial));
        stack.Children.Add(Row("Firmware Sürümü", _firmware));
        stack.Children.Add(Row("ICCID", _iccid));
        stack.Children.Add(Row("IMSI", _imsi));
        stack.Children.Add(Row("Meter Type", _meterType));
        stack.Children.Add(Row("Meter Seri No", _meterSerial));

        stack.Children.Add(Section("Davranış"));
        stack.Children.Add(Row("Konnektör Sayısı", _connectors));
        stack.Children.Add(Row("", _autoBoot));
        stack.Children.Add(Row("", _autoMeter));

        stack.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 10, 0, 0),
            Children = { _connectBtn, _disconnectBtn, _bootBtn },
        });

        return new ScrollViewer { Content = stack };
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
        _autoBoot.IsChecked = s.AutoBootOnConnect;
        _autoMeter.IsChecked = s.AutoMeterValues;
    }

    private void ApplyToSettings()
    {
        var s = Cp.Settings;
        s.CentralSystemUrl = _url.Text?.Trim() ?? "";
        s.ChargePointId = _cpId.Text?.Trim() ?? "";
        s.BasicAuthUser = Empty(_authUser.Text);
        s.BasicAuthPassword = Empty(_authPass.Text);
        s.ChargePointVendor = _vendor.Text?.Trim() ?? "";
        s.ChargePointModel = _model.Text?.Trim() ?? "";
        s.ChargePointSerialNumber = Empty(_serial.Text);
        s.ChargeBoxSerialNumber = Empty(_chargeBoxSerial.Text);
        s.FirmwareVersion = Empty(_firmware.Text);
        s.Iccid = Empty(_iccid.Text);
        s.Imsi = Empty(_imsi.Text);
        s.MeterType = Empty(_meterType.Text);
        s.MeterSerialNumber = Empty(_meterSerial.Text);

        var newCount = (int)(_connectors.Value ?? 2);
        if (newCount != s.NumberOfConnectors)
        {
            s.NumberOfConnectors = newCount;
            Cp.RebuildConnectors();
        }
        s.AutoBootOnConnect = _autoBoot.IsChecked == true;
        s.AutoMeterValues = _autoMeter.IsChecked == true;
        SettingsStore.Save(s);
    }

    private static string? Empty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void OnConnect()
    {
        ApplyToSettings();
        Run(() => Cp.ConnectAsync());
    }

    public override void RefreshUi()
    {
        var connected = Cp.IsConnected;
        _connectBtn.IsEnabled = !connected;
        _disconnectBtn.IsEnabled = connected;
        _bootBtn.IsEnabled = connected;
    }

    private static Control Section(string title) => new TextBlock
    {
        Text = title, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 12, 0, 4),
    };

    private static Control Row(string label, Control control) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Children =
        {
            new TextBlock { Text = label, Width = 190, VerticalAlignment = VerticalAlignment.Center },
            control,
        },
    };
}
