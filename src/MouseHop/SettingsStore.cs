using System.Text.Json;

namespace MouseHop;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseHop");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    internal static HotKeySettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return HotKeySettings.Default;
            }

            var json = File.ReadAllText(SettingsPath);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json);
            if (dto is null || dto.Key == Keys.None)
            {
                return HotKeySettings.Default;
            }

            return new HotKeySettings(dto.Modifiers, dto.Key);
        }
        catch
        {
            return HotKeySettings.Default;
        }
    }

    internal static void Save(HotKeySettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var dto = new SettingsDto(settings.Modifiers, settings.Key);
        var json = JsonSerializer.Serialize(dto, SerializerOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private sealed record SettingsDto(uint Modifiers, Keys Key);
}
