using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ocpp.Core.Domain;
using OcppSimulator.Mac.Tabs;

namespace OcppSimulator.Mac;

/// <summary>
/// Main window of the cross-platform (macOS/Windows/Linux) OCPP 1.6 Charge Point simulator.
/// Owns the <see cref="ChargePoint"/>, marshals its events to the UI thread and hosts the tabs.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly ChargePoint _cp;
    private readonly List<ChargePointTab> _tabs = new();
    private readonly LogTab _logTab = new();

    private readonly TextBlock _connectionLabel = new() { Text = "Bağlı değil", Foreground = Brushes.IndianRed, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _bootLabel = new() { Text = "", VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _notifyLabel = new() { Text = "", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };

    public MainWindow()
    {
        var settings = SettingsStore.Load();
        _cp = new ChargePoint(settings);

        Title = "OCPP 1.6 Sanal Şarj İstasyonu Simülatörü — CW Enerji";
        Width = 1100;
        Height = 820;
        MinWidth = 900;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TryLoadIcon();

        Content = BuildLayout();
        WireEvents();

        foreach (var t in _tabs) { t.ErrorSink = ShowNotify; t.Bind(_cp); }
        _logTab.Bind(_cp);
        UpdateConnectionUi(_cp.IsConnected);
    }

    private void TryLoadIcon()
    {
        try
        {
            using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://OcppSimulator.Mac/Assets/appicon.png"));
            Icon = new WindowIcon(new Avalonia.Media.Imaging.Bitmap(stream));
        }
        catch { /* fall back to default icon */ }
    }

    private Control BuildLayout()
    {
        var tabControl = new TabControl { Padding = new Thickness(0) };
        AddTab(tabControl, "Bağlantı", new ConnectionTab());
        AddTab(tabControl, "Konnektörler", new ConnectorsTab());
        AddTab(tabControl, "Mesajlar", new MessagesTab());
        AddTab(tabControl, "Konfigürasyon", new ConfigurationTab());
        AddTab(tabControl, "Local Auth List", new LocalAuthListTab());
        AddTab(tabControl, "Rezervasyonlar", new ReservationsTab());
        AddTab(tabControl, "Smart Charging", new SmartChargingTab());
        AddTab(tabControl, "Firmware / Diagnostics", new FirmwareDiagnosticsTab());
        tabControl.Items.Add(new TabItem { Header = "Log", Content = _logTab });

        var statusBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    _connectionLabel,
                    new TextBlock { Text = "|", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center },
                    _bootLabel,
                    new TextBlock { Text = "|", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center },
                    _notifyLabel,
                },
            },
        };

        var dock = new DockPanel();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        dock.Children.Add(statusBar);
        dock.Children.Add(tabControl); // fills remaining
        return dock;
    }

    private void AddTab(TabControl host, string header, ChargePointTab tab)
    {
        host.Items.Add(new TabItem { Header = header, Content = tab });
        _tabs.Add(tab);
    }

    private void WireEvents()
    {
        _cp.Log += entry => UiHelpers.RunOnUi(() => _logTab.Append(entry));
        _cp.StateChanged += () => UiHelpers.RunOnUi(RefreshAll);
        _cp.Notification += msg => UiHelpers.RunOnUi(() => ShowNotify(msg));
        _cp.ConnectionStateChanged += connected => UiHelpers.RunOnUi(() => UpdateConnectionUi(connected));
        Closing += (_, _) => { SettingsStore.Save(_cp.Settings); _ = _cp.DisposeAsync(); };
    }

    private void RefreshAll()
    {
        foreach (var t in _tabs) t.RefreshUi();
        _bootLabel.Text = _cp.LastBootStatus is { } s ? $"Boot: {s}" : "";
    }

    private void ShowNotify(string message) => _notifyLabel.Text = message;

    private void UpdateConnectionUi(bool connected)
    {
        _connectionLabel.Text = connected ? $"Bağlı — {_cp.Settings.ChargePointId}" : "Bağlı değil";
        _connectionLabel.Foreground = connected ? Brushes.SeaGreen : Brushes.IndianRed;
        RefreshAll();
    }
}
