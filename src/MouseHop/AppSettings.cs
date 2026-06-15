namespace MouseHop;

internal sealed record AppSettings(
    HotKeySettings HotKey,
    MovementMode MovementMode,
    IReadOnlyList<string> DisplayOrder)
{
    internal static AppSettings Default { get; } = new(
        HotKeySettings.Default,
        MovementMode.Loop,
        Array.Empty<string>());
}
