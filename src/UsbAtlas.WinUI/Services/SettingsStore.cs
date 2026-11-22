using System.Text.Json;
using System.Text.Json.Serialization;
using UsbAtlas.WinUI.Models;

namespace UsbAtlas.WinUI.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsStore()
    {
        SettingsPath = ResolveSettingsPath();
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), Options) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // Settings are a convenience; a read-only profile must not break the app.
        }
    }

    private static string ResolveSettingsPath()
    {
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UsbAtlas");
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "settings.json");
        }
        catch
        {
            // Some locked-down profiles deny writes under LocalAppData.
            // Fall back to the application folder so the UI still starts and settings persist.
            return Path.Combine(AppContext.BaseDirectory, "settings.json");
        }
    }
}
