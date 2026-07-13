using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ocpp.Core.Types;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Firmware &amp; diagnostics status, with manual status-notification sending.</summary>
public sealed class FirmwareDiagnosticsTab : ChargePointTab
{
    private readonly TextBlock _firmwareStatus = new() { FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _diagnosticsStatus = new() { FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox _firmwareCombo = UiHelpers.EnumCombo(FirmwareStatus.Idle);
    private readonly ComboBox _diagCombo = UiHelpers.EnumCombo(DiagnosticsStatus.Idle);

    protected override Control Build()
    {
        var stack = new StackPanel { Margin = new Thickness(14), Spacing = 8 };

        stack.Children.Add(Header("Firmware"));
        stack.Children.Add(Line(UiHelpers.Label("Mevcut durum:"), _firmwareStatus));
        stack.Children.Add(Line(
            UiHelpers.Label("FirmwareStatusNotification:"), _firmwareCombo,
            UiHelpers.Button("Gönder", () => Run(() => Cp.SendFirmwareStatusNotificationAsync(_firmwareCombo.SelectedEnum<FirmwareStatus>())), 100)));

        stack.Children.Add(Header("Diagnostics"));
        stack.Children.Add(Line(UiHelpers.Label("Mevcut durum:"), _diagnosticsStatus));
        stack.Children.Add(Line(
            UiHelpers.Label("DiagnosticsStatusNotification:"), _diagCombo,
            UiHelpers.Button("Gönder", () => Run(() => Cp.SendDiagnosticsStatusNotificationAsync(_diagCombo.SelectedEnum<DiagnosticsStatus>())), 100)));

        stack.Children.Add(new TextBlock
        {
            Foreground = Brushes.DimGray, Margin = new Thickness(0, 16, 0, 0), TextWrapping = TextWrapping.Wrap,
            Text = "CSMS'ten gelen UpdateFirmware ve GetDiagnostics komutları otomatik olarak ilgili durum bildirimi dizisini (Downloading→Installed / Uploading→Uploaded) üretir.",
        });

        return new ScrollViewer { Content = stack };
    }

    private static Control Header(string title) => new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 15, Margin = new Thickness(0, 8, 0, 4) };

    private static Control Line(params Control[] controls)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var c in controls) { c.VerticalAlignment = VerticalAlignment.Center; p.Children.Add(c); }
        return p;
    }

    public override void RefreshUi()
    {
        _firmwareStatus.Text = Cp.FirmwareStatus.ToString();
        _diagnosticsStatus.Text = Cp.DiagnosticsStatus.ToString();
    }
}
