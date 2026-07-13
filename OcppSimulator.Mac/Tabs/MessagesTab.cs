using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ocpp.Core.Messages;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Manually send any CP-initiated message with an editable JSON payload and see the response.</summary>
public sealed class MessagesTab : ChargePointTab
{
    private readonly ComboBox _action = new() { Width = 280 };
    private readonly TextBox _payload = new()
    {
        AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Consolas, Menlo, monospace"), FontSize = 13,
    };
    private readonly TextBox _response = new()
    {
        AcceptsReturn = true, IsReadOnly = true, TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Consolas, Menlo, monospace"), FontSize = 13,
        Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
    };
    private Button _send = null!;

    private static readonly (string Action, string Sample)[] Templates =
    {
        (OcppAction.BootNotification, "{\n  \"chargePointVendor\": \"CW Enerji\",\n  \"chargePointModel\": \"CW-SIM-1\"\n}"),
        (OcppAction.Heartbeat, "{}"),
        (OcppAction.Authorize, "{\n  \"idTag\": \"RFID-001\"\n}"),
        (OcppAction.StatusNotification, "{\n  \"connectorId\": 1,\n  \"errorCode\": \"NoError\",\n  \"status\": \"Available\"\n}"),
        (OcppAction.StartTransaction, "{\n  \"connectorId\": 1,\n  \"idTag\": \"RFID-001\",\n  \"meterStart\": 0,\n  \"timestamp\": \"2026-07-10T10:00:00Z\"\n}"),
        (OcppAction.StopTransaction, "{\n  \"transactionId\": 1,\n  \"meterStop\": 1000,\n  \"timestamp\": \"2026-07-10T11:00:00Z\",\n  \"reason\": \"Local\"\n}"),
        (OcppAction.MeterValues, "{\n  \"connectorId\": 1,\n  \"meterValue\": [\n    {\n      \"timestamp\": \"2026-07-10T10:30:00Z\",\n      \"sampledValue\": [ { \"value\": \"500\", \"measurand\": \"Energy.Active.Import.Register\", \"unit\": \"Wh\" } ]\n    }\n  ]\n}"),
        (OcppAction.DataTransfer, "{\n  \"vendorId\": \"CW\",\n  \"messageId\": \"ping\",\n  \"data\": \"hello\"\n}"),
        (OcppAction.DiagnosticsStatusNotification, "{\n  \"status\": \"Idle\"\n}"),
        (OcppAction.FirmwareStatusNotification, "{\n  \"status\": \"Idle\"\n}"),
    };

    protected override Control Build()
    {
        foreach (var t in Templates) _action.Items.Add(t.Action);
        _action.SelectionChanged += (_, _) => LoadTemplate();
        _action.SelectedIndex = 0;
        _send = UiHelpers.Button("Gönder", OnSend, 120);

        var top = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(6),
            Children = { UiHelpers.Label("Mesaj (CP → CS)"), _action, _send },
        };

        var reqGroup = Group("İstek Payload (JSON)", _payload);
        var respGroup = Group("Yanıt", _response);

        var grid = new Grid { RowDefinitions = new RowDefinitions("*,*") };
        Grid.SetRow(reqGroup, 0);
        Grid.SetRow(respGroup, 1);
        grid.Children.Add(reqGroup);
        grid.Children.Add(respGroup);

        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top);
        dock.Children.Add(top);
        dock.Children.Add(grid);
        LoadTemplate();
        return dock;
    }

    private static Control Group(string header, Control content)
    {
        var dock = new DockPanel { Margin = new Thickness(6) };
        var title = new TextBlock { Text = header, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(title, Dock.Top);
        dock.Children.Add(title);
        dock.Children.Add(content);
        return dock;
    }

    private void LoadTemplate()
    {
        var action = _action.SelectedItem?.ToString();
        var tpl = Templates.FirstOrDefault(t => t.Action == action);
        _payload.Text = tpl.Sample ?? "{}";
    }

    private void OnSend()
    {
        var action = _action.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(action)) return;
        _response.Text = "Gönderiliyor...";
        Run(async () =>
        {
            var resp = await Cp.SendRawAsync(action, _payload.Text ?? "{}");
            _response.Text = string.IsNullOrEmpty(resp) ? "(boş yanıt)" : resp;
        });
    }

    public override void RefreshUi() => _send.IsEnabled = Cp.IsConnected;
}
