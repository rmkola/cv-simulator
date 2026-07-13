using System;
using System.IO;
using System.Text.Json;
using Ocpp.Core.Domain;

namespace OcppSimulator.Mac;

/// <summary>Loads/saves <see cref="ChargePointSettings"/> under the user's app-data directory
/// (cross-platform: %AppData% on Windows, ~/.config or ~/Library/Application Support on macOS/Linux).</summary>
public static class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OcppSimulator");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static ChargePointSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<ChargePointSettings>(File.ReadAllText(FilePath)) ?? new ChargePointSettings();
        }
        catch { /* fall back to defaults */ }
        return new ChargePointSettings();
    }

    public static void Save(ChargePointSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch { /* non-fatal */ }
    }
}
