using System.IO;
using System.Text.Json;
using Ocpp.Core.Domain;

namespace OcppSimulator.App;

/// <summary>Loads/saves <see cref="ChargePointSettings"/> to %AppData%\OcppSimulator\settings.json.</summary>
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
