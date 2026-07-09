using Ocpp.Core.Domain;
using Ocpp.Core.Protocol;
using OcppSimulator.App.Tabs;

namespace OcppSimulator.App;

/// <summary>
/// Main window of the OCPP 1.6 Charge Point simulator. Owns the <see cref="ChargePoint"/>, marshals
/// its events to the UI thread, and hosts the feature tabs.
/// </summary>
public sealed class MainForm : Form
{
    private readonly ChargePoint _cp;
    private readonly List<ChargePointTab> _tabs = new();
    private readonly LogTab _logTab = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _connectionLabel = new() { Text = "Bağlı değil" };
    private readonly ToolStripStatusLabel _bootLabel = new() { Text = "" };
    private readonly ToolStripStatusLabel _notifyLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    public MainForm()
    {
        var settings = SettingsStore.Load();
        _cp = new ChargePoint(settings);

        Text = "OCPP 1.6 Sanal Şarj İstasyonu Simülatörü — CW Enerji";
        Width = 1100;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        TryLoadIcon();

        BuildTabs();
        BuildStatusStrip();
        WireEvents();

        foreach (var t in _tabs) t.Bind(_cp);
        _logTab.Bind(_cp);
        UpdateConnectionUi(_cp.IsConnected);
    }

    /// <summary>Uses the executable's own icon (set via ApplicationIcon) for the window/taskbar.</summary>
    private void TryLoadIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
        }
        catch { /* fall back to default icon */ }
    }

    private void BuildTabs()
    {
        var tabControl = new TabControl { Dock = DockStyle.Fill };

        AddTab(tabControl, "Bağlantı", new ConnectionTab());
        AddTab(tabControl, "Konnektörler", new ConnectorsTab());
        AddTab(tabControl, "Mesajlar", new MessagesTab());
        AddTab(tabControl, "Konfigürasyon", new ConfigurationTab());
        AddTab(tabControl, "Local Auth List", new LocalAuthListTab());
        AddTab(tabControl, "Rezervasyonlar", new ReservationsTab());
        AddTab(tabControl, "Smart Charging", new SmartChargingTab());
        AddTab(tabControl, "Firmware / Diagnostics", new FirmwareDiagnosticsTab());

        // Log tab (not state-driven the same way; receives entries directly).
        var logPage = new TabPage("Log");
        _logTab.Dock = DockStyle.Fill;
        logPage.Controls.Add(_logTab);
        tabControl.TabPages.Add(logPage);

        Controls.Add(tabControl);
    }

    private void AddTab(TabControl host, string title, ChargePointTab tab)
    {
        var page = new TabPage(title);
        tab.Dock = DockStyle.Fill;
        page.Controls.Add(tab);
        host.TabPages.Add(page);
        _tabs.Add(tab);
    }

    private void BuildStatusStrip()
    {
        _statusStrip.Items.Add(_connectionLabel);
        _statusStrip.Items.Add(new ToolStripStatusLabel("|"));
        _statusStrip.Items.Add(_bootLabel);
        _statusStrip.Items.Add(new ToolStripStatusLabel("|"));
        _statusStrip.Items.Add(_notifyLabel);
        Controls.Add(_statusStrip);
    }

    private void WireEvents()
    {
        _cp.Log += entry => this.RunOnUi(() => _logTab.Append(entry));
        _cp.StateChanged += () => this.RunOnUi(RefreshAll);
        _cp.Notification += msg => this.RunOnUi(() => _notifyLabel.Text = msg);
        _cp.ConnectionStateChanged += connected => this.RunOnUi(() => UpdateConnectionUi(connected));
        FormClosing += (_, _) => { SettingsStore.Save(_cp.Settings); _ = _cp.DisposeAsync(); };
    }

    private void RefreshAll()
    {
        foreach (var t in _tabs) t.RefreshUi();
        _bootLabel.Text = _cp.LastBootStatus is { } s ? $"Boot: {s}" : "";
    }

    private void UpdateConnectionUi(bool connected)
    {
        _connectionLabel.Text = connected ? $"Bağlı — {_cp.Settings.ChargePointId}" : "Bağlı değil";
        _connectionLabel.ForeColor = connected ? Color.Green : Color.Firebrick;
        RefreshAll();
    }
}
