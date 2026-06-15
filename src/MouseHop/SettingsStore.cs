using System.Text.Json;

namespace MouseHop;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseHop");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    internal static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return AppSettings.Default;
            }

            var json = File.ReadAllText(SettingsPath);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json);
            if (dto is null || dto.Key == Keys.None)
            {
                return AppSettings.Default;
            }

            var movementMode = Enum.IsDefined(typeof(MovementMode), dto.MovementMode)
                ? dto.MovementMode
                : MovementMode.Loop;

            return new AppSettings(
                new HotKeySettings(dto.Modifiers, dto.Key),
                movementMode,
                dto.DisplayOrder ?? Array.Empty<string>());
        }
        catch
        {
            return AppSettings.Default;
        }
    }

    internal static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var dto = new SettingsDto
        {
            Modifiers = settings.HotKey.Modifiers,
            Key = settings.HotKey.Key,
            MovementMode = settings.MovementMode,
            DisplayOrder = settings.DisplayOrder.ToArray()
        };
        var json = JsonSerializer.Serialize(dto, SerializerOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private sealed class SettingsDto
    {
        public uint Modifiers { get; set; }

        public Keys Key { get; set; }

        public MovementMode MovementMode { get; set; } = MovementMode.Loop;

        public string[]? DisplayOrder { get; set; }
    }
}