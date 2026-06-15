namespace MouseHop;

internal sealed record HotKeySettings(uint Modifiers, Keys Key)
{
    internal static HotKeySettings Default { get; } = new(
        NativeMethods.ModControl | NativeMethods.ModAlt,
        Keys.F12);

    internal string DisplayText
    {
        get
        {
            var parts = new List<string>();

            if ((Modifiers & NativeMethods.ModControl) != 0)
            {
                parts.Add("Ctrl");
            }

            if ((Modifiers & NativeMethods.ModAlt) != 0)
            {
                parts.Add("Alt");
            }

            if ((Modifiers & NativeMethods.ModShift) != 0)
            {
                parts.Add("Shift");
            }

            if ((Modifiers & NativeMethods.ModWin) != 0)
            {
                parts.Add("Win");
            }

            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }
    }
}
