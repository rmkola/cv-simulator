using Ocpp.Core.Domain;

namespace OcppSimulator.App;

/// <summary>Small helpers for building the Windows Forms UI in code.</summary>
public static class UiHelpers
{
    public static void RunOnUi(this Control control, Action action)
    {
        if (control.IsDisposed || control.Disposing) return;
        if (control.InvokeRequired) control.BeginInvoke(action);
        else action();
    }

    public static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 6, 3, 3),
    };

    public static TextBox MakeTextBox(string? value = null, bool multiline = false) => new()
    {
        Text = value ?? "",
        Width = 260,
        Multiline = multiline,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(3, 3, 3, 3),
    };

    public static Button MakeButton(string text, EventHandler onClick, int width = 150)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoEllipsis = false,
            MinimumSize = new Size(width, 30),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(3),
        };
        b.Click += onClick;
        return b;
    }

    public static ComboBox MakeEnumCombo<TEnum>(TEnum selected) where TEnum : struct, Enum
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(3) };
        cb.Items.AddRange(Enum.GetNames<TEnum>().Cast<object>().ToArray());
        cb.SelectedItem = selected.ToString();
        return cb;
    }

    public static TEnum SelectedEnum<TEnum>(this ComboBox cb) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(cb.SelectedItem?.ToString(), out var v) ? v : default;
}

/// <summary>Base for a tab that observes a <see cref="ChargePoint"/> and re-renders on state changes.</summary>
public abstract class ChargePointTab : UserControl
{
    protected ChargePoint Cp { get; private set; } = null!;

    public void Bind(ChargePoint cp)
    {
        Cp = cp;
        OnBound();
        RefreshUi();
    }

    /// <summary>Called once after the ChargePoint is bound; build event subscriptions here if needed.</summary>
    protected virtual void OnBound() { }

    /// <summary>Re-render from current ChargePoint state. Always invoked on the UI thread.</summary>
    public virtual void RefreshUi() { }

    /// <summary>Fire-and-forget an async CP operation, surfacing errors as a message box.</summary>
    protected async void Run(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "OCPP", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}
