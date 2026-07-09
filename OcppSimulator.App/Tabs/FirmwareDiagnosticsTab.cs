using Ocpp.Core.Types;

namespace OcppSimulator.App.Tabs;

/// <summary>Firmware &amp; diagnostics status, with manual status-notification sending.</summary>
public sealed class FirmwareDiagnosticsTab : ChargePointTab
{
    private readonly Label _firmwareStatus = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
    private readonly Label _diagnosticsStatus = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
    private readonly ComboBox _firmwareCombo = UiHelpers.MakeEnumCombo(FirmwareStatus.Idle);
    private readonly ComboBox _diagCombo = UiHelpers.MakeEnumCombo(DiagnosticsStatus.Idle);

    public FirmwareDiagnosticsTab()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(12), AutoSize = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddHeader(layout, "Firmware");
        layout.Controls.Add(UiHelpers.MakeLabel("Mevcut durum:"), 0, layout.RowCount);
        layout.Controls.Add(_firmwareStatus, 1, layout.RowCount++);
        layout.Controls.Add(UiHelpers.MakeLabel("FirmwareStatusNotification:"), 0, layout.RowCount);
        layout.Controls.Add(_firmwareCombo, 1, layout.RowCount);
        layout.Controls.Add(UiHelpers.MakeButton("Gönder", (_, _) =>
            Run(() => Cp.SendFirmwareStatusNotificationAsync(_firmwareCombo.SelectedEnum<FirmwareStatus>())), 100), 2, layout.RowCount++);

        AddHeader(layout, "Diagnostics");
        layout.Controls.Add(UiHelpers.MakeLabel("Mevcut durum:"), 0, layout.RowCount);
        layout.Controls.Add(_diagnosticsStatus, 1, layout.RowCount++);
        layout.Controls.Add(UiHelpers.MakeLabel("DiagnosticsStatusNotification:"), 0, layout.RowCount);
        layout.Controls.Add(_diagCombo, 1, layout.RowCount);
        layout.Controls.Add(UiHelpers.MakeButton("Gönder", (_, _) =>
            Run(() => Cp.SendDiagnosticsStatusNotificationAsync(_diagCombo.SelectedEnum<DiagnosticsStatus>())), 100), 2, layout.RowCount++);

        var note = new Label
        {
            AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 16, 3, 3),
            Text = "CSMS'ten gelen UpdateFirmware ve GetDiagnostics komutları otomatik olarak\nilgili durum bildirimi dizisini (Downloading→Installed / Uploading→Uploaded) üretir.",
        };
        layout.Controls.Add(note, 0, layout.RowCount);
        layout.SetColumnSpan(note, 3);

        Controls.Add(layout);
    }

    private static void AddHeader(TableLayoutPanel layout, string title)
    {
        var lbl = new Label { Text = title, AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(3, 12, 3, 6) };
        layout.Controls.Add(lbl, 0, layout.RowCount++);
        layout.SetColumnSpan(lbl, 3);
    }

    public override void RefreshUi()
    {
        _firmwareStatus.Text = Cp.FirmwareStatus.ToString();
        _diagnosticsStatus.Text = Cp.DiagnosticsStatus.ToString();
    }
}
