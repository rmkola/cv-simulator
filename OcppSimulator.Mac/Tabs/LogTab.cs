using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ocpp.Core.Protocol;

namespace OcppSimulator.Mac.Tabs;

/// <summary>Raw OCPP-J traffic log with clear/export and pause.</summary>
public sealed class LogTab : ChargePointTab
{
    private readonly TextBox _box = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Consolas, Menlo, monospace"),
        FontSize = 12,
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
        Foreground = Brushes.Gainsboro,
    };
    private readonly CheckBox _autoScroll = new() { Content = "Otomatik kaydır", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _pause = new() { Content = "Duraklat", VerticalAlignment = VerticalAlignment.Center };
    private readonly StringBuilder _buffer = new();
    private int _lines;

    protected override Control Build()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Avalonia.Thickness(6),
            Children =
            {
                UiHelpers.Button("Temizle", () => { _buffer.Clear(); _box.Text = ""; _lines = 0; }, 90),
                UiHelpers.Button("Dışa Aktar", ExportAsync, 100),
                _autoScroll,
                _pause,
            },
        };

        var scroller = new ScrollViewer
        {
            Content = _box,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        var dock = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        dock.Children.Add(toolbar);
        dock.Children.Add(scroller);
        return dock;
    }

    public void Append(OcppLogEntry entry)
    {
        if (_pause.IsChecked == true) return;
        if (_lines > 4000) { _buffer.Clear(); _lines = 0; }

        var prefix = entry.Direction switch
        {
            LogDirection.Outgoing => "→ CS",
            LogDirection.Incoming => "← CS",
            _ => "  ·",
        };
        var meta = entry.Action is not null ? $"{entry.Action} " : "";
        _buffer.Append($"{entry.Timestamp.UtcDateTime:HH:mm:ss.fff}Z {prefix} {meta}{entry.Raw}\n");
        _lines++;

        _box.Text = _buffer.ToString();
        if (_autoScroll.IsChecked == true)
            _box.CaretIndex = _box.Text?.Length ?? 0;
    }

    private async void ExportAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "ocpp-traffic.log",
            FileTypeChoices = new[] { new FilePickerFileType("Log") { Patterns = new[] { "*.log", "*.txt" } } },
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(_buffer.ToString());
    }
}
