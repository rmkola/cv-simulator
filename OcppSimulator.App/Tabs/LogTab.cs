using System.Text;
using Ocpp.Core.Protocol;

namespace OcppSimulator.App.Tabs;

/// <summary>Raw OCPP-J traffic log with direction colouring, pause, clear and export.</summary>
public sealed class LogTab : ChargePointTab
{
    private readonly RichTextBox _box = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9.5f),
        BackColor = Color.FromArgb(24, 24, 24), ForeColor = Color.Gainsboro, WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both,
    };
    private readonly CheckBox _autoScroll = new() { Text = "Otomatik kaydır", Checked = true, AutoSize = true };
    private readonly CheckBox _pause = new() { Text = "Duraklat", AutoSize = true };
    private int _lineCount;

    public LogTab()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4) };
        toolbar.Controls.Add(UiHelpers.MakeButton("Temizle", (_, _) => { _box.Clear(); _lineCount = 0; }, 90));
        toolbar.Controls.Add(UiHelpers.MakeButton("Dışa Aktar", (_, _) => Export(), 100));
        toolbar.Controls.Add(_autoScroll);
        toolbar.Controls.Add(_pause);

        Controls.Add(_box);
        Controls.Add(toolbar);
    }

    public void Append(OcppLogEntry entry)
    {
        if (_pause.Checked) return;
        if (_lineCount > 4000) { _box.Clear(); _lineCount = 0; }

        var (prefix, color) = entry.Direction switch
        {
            LogDirection.Outgoing => ("→ CS", Color.FromArgb(120, 190, 255)),
            LogDirection.Incoming => ("← CS", Color.FromArgb(150, 230, 150)),
            _ => ("  ·", Color.Gray),
        };

        var header = $"{entry.Timestamp.UtcDateTime:HH:mm:ss.fff}Z {prefix} ";
        var meta = entry.Action is not null ? $"{entry.Action} " : "";
        AppendColored(header + meta, color);
        AppendColored(entry.Raw + Environment.NewLine, entry.Direction == LogDirection.Info ? Color.Gray : Color.Gainsboro);
        _lineCount++;

        if (_autoScroll.Checked)
        {
            _box.SelectionStart = _box.TextLength;
            _box.ScrollToCaret();
        }
    }

    private void AppendColored(string text, Color color)
    {
        _box.SelectionStart = _box.TextLength;
        _box.SelectionLength = 0;
        _box.SelectionColor = color;
        _box.AppendText(text);
        _box.SelectionColor = _box.ForeColor;
    }

    private void Export()
    {
        using var dlg = new SaveFileDialog { Filter = "Log dosyası (*.log)|*.log|Metin (*.txt)|*.txt", FileName = "ocpp-traffic.log" };
        if (dlg.ShowDialog() == DialogResult.OK)
            System.IO.File.WriteAllText(dlg.FileName, _box.Text, Encoding.UTF8);
    }
}
