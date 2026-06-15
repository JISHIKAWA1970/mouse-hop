namespace MouseHop;

internal sealed record AppSettings(HotKeySettings HotKey, MovementMode MovementMode)
{
    internal static AppSettings Default { get; } = new(HotKeySettings.Default, MovementMode.Loop);
}
