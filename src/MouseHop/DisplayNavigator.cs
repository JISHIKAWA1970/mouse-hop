namespace MouseHop;

internal sealed class DisplayNavigator
{
    private int fallbackIndex = -1;

    internal bool MoveToNextDisplayCenter()
    {
        var screens = GetOrderedScreens();
        if (screens.Length == 0)
        {
            return false;
        }

        var currentPosition = Cursor.Position;
        var currentIndex = Array.FindIndex(screens, screen => screen.Bounds.Contains(currentPosition));
        int nextIndex;

        if (currentIndex >= 0)
        {
            nextIndex = (currentIndex + 1) % screens.Length;
            fallbackIndex = nextIndex;
        }
        else
        {
            fallbackIndex = (fallbackIndex + 1) % screens.Length;
            nextIndex = fallbackIndex;
        }

        var bounds = screens[nextIndex].Bounds;
        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2;

        return NativeMethods.SetCursorPos(centerX, centerY);
    }

    private static Screen[] GetOrderedScreens()
    {
        return Screen.AllScreens
            .OrderBy(screen => screen.Bounds.Top)
            .ThenBy(screen => screen.Bounds.Left)
            .ThenBy(screen => screen.DeviceName, StringComparer.Ordinal)
            .ToArray();
    }
}
