using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Ocpp.Core.Domain;

namespace OcppSimulator.Mac;

/// <summary>Helpers for building the Avalonia UI in code and marshaling to the UI thread.</summary>
public static class UiHelpers
{
    /// <summary>Runs <paramref name="action"/> on the UI thread (safe from any thread).</summary>
    public static void RunOnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Avalonia.Thickness(0, 0, 8, 0),
    };

    public static TextBox TextBox(string? value = null, double width = 260) => new()
    {
        Text = value ?? "",
        Width = width,
        Margin = new Avalonia.Thickness(0, 3, 6, 3),
    };

    public static Button Button(string text, Action onClick, double minWidth = 130)
    {
        var b = new Button
        {
            Content = text,
            MinWidth = minWidth,
            Margin = new Avalonia.Thickness(0, 3, 6, 3),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    public static ComboBox EnumCombo<TEnum>(TEnum selected) where TEnum : struct, Enum
    {
        var cb = new ComboBox { Width = 200, Margin = new Avalonia.Thickness(0, 3, 6, 3) };
        foreach (var name in Enum.GetNames<TEnum>()) cb.Items.Add(name);
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
        Content = Build();
        OnBound();
        RefreshUi();
    }

    /// <summary>Builds the tab content (called once after binding).</summary>
    protected abstract Control Build();

    protected virtual void OnBound() { }

    /// <summary>Re-render from current ChargePoint state. Always invoked on the UI thread.</summary>
    public virtual void RefreshUi() { }

    /// <summary>Fire-and-forget an async CP operation, surfacing errors to the notify callback.</summary>
    protected async void Run(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { Cp?.RaiseStateChanged(); ShowError(ex.Message); }
    }

    /// <summary>Overridable error surface (MainWindow shows it in the status bar).</summary>
    public Action<string>? ErrorSink { get; set; }
    private void ShowError(string message) => ErrorSink?.Invoke(message);
}
