using Ocpp.Core.Messages;

namespace OcppSimulator.App.Tabs;

/// <summary>Manually send any CP-initiated message with an editable JSON payload and see the response.</summary>
public sealed class MessagesTab : ChargePointTab
{
    private readonly ComboBox _action = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly TextBox _payload = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9.5f) };
    private readonly TextBox _response = new() { Multiline = true, Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9.5f), BackColor = Color.FromArgb(245, 245, 245) };
    private readonly Button _send;

    private static readonly (string Action, string Sample)[] Templates =
    {
        (OcppAction.BootNotification, "{\n  \"chargePointVendor\": \"CW Enerji\",\n  \"chargePointModel\": \"CW-SIM-1\"\n}"),
        (OcppAction.Heartbeat, "{}"),
        (OcppAction.Authorize, "{\n  \"idTag\": \"RFID-001\"\n}"),
        (OcppAction.StatusNotification, "{\n  \"connectorId\": 1,\n  \"errorCode\": \"NoError\",\n  \"status\": \"Available\"\n}"),
        (OcppAction.StartTransaction, "{\n  \"connectorId\": 1,\n  \"idTag\": \"RFID-001\",\n  \"meterStart\": 0,\n  \"timestamp\": \"2026-07-09T10:00:00Z\"\n}"),
        (OcppAction.StopTransaction, "{\n  \"transactionId\": 1,\n  \"meterStop\": 1000,\n  \"timestamp\": \"2026-07-09T11:00:00Z\",\n  \"reason\": \"Local\"\n}"),
        (OcppAction.MeterValues, "{\n  \"connectorId\": 1,\n  \"meterValue\": [\n    {\n      \"timestamp\": \"2026-07-09T10:30:00Z\",\n      \"sampledValue\": [ { \"value\": \"500\", \"measurand\": \"Energy.Active.Import.Register\", \"unit\": \"Wh\" } ]\n    }\n  ]\n}"),
        (OcppAction.DataTransfer, "{\n  \"vendorId\": \"CW\",\n  \"messageId\": \"ping\",\n  \"data\": \"hello\"\n}"),
        (OcppAction.DiagnosticsStatusNotification, "{\n  \"status\": \"Idle\"\n}"),
        (OcppAction.FirmwareStatusNotification, "{\n  \"status\": \"Idle\"\n}"),
    };

    public MessagesTab()
    {
        _action.Items.AddRange(Templates.Select(t => (object)t.Action).ToArray());
        _action.SelectedIndexChanged += (_, _) => LoadTemplate();
        _action.SelectedIndex = 0;
        _send = UiHelpers.MakeButton("Gönder", (_, _) => OnSend(), 120);

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        top.Controls.Add(UiHelpers.MakeLabel("Mesaj (CP → CS)"));
        top.Controls.Add(_action);
        top.Controls.Add(_send);

        var reqGroup = new GroupBox { Text = "İstek Payload (JSON)", Dock = DockStyle.Fill };
        reqGroup.Controls.Add(_payload);
        var respGroup = new GroupBox { Text = "Yanıt", Dock = DockStyle.Fill };
        respGroup.Controls.Add(_response);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(reqGroup);
        split.Panel2.Controls.Add(respGroup);

        Controls.Add(split);
        Controls.Add(top);
        LoadTemplate();
    }

    private void LoadTemplate()
    {
        var tpl = Templates.FirstOrDefault(t => t.Action == _action.SelectedItem?.ToString());
        _payload.Text = tpl.Sample ?? "{}";
    }

    private void OnSend()
    {
        var action = _action.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(action)) return;
        _response.Text = "Gönderiliyor...";
        Run(async () =>
        {
            var resp = await Cp.SendRawAsync(action, _payload.Text);
            _response.Text = string.IsNullOrEmpty(resp) ? "(boş yanıt)" : resp;
        });
    }

    public override void RefreshUi() => _send.Enabled = Cp.IsConnected;
}
