namespace MouseHop;

internal sealed class DisplayNavigator
{
    private int fallbackIndex = -1;
    private int direction = 1;

    internal bool MoveToNextDisplayCenter(MovementMode movementMode)
    {
        var screens = GetOrderedScreens();
        if (screens.Length == 0)
        {
            return false;
        }

        var currentPosition = Cursor.Position;
        var currentIndex = Array.FindIndex(screens, screen => screen.Bounds.Contains(currentPosition));
        var nextIndex = currentIndex >= 0
            ? GetNextIndex(currentIndex, screens.Length, movementMode)
            : GetFallbackNextIndex(screens.Length, movementMode);

        fallbackIndex = nextIndex;

        var bounds = screens[nextIndex].Bounds;
        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2;

        return NativeMethods.SetCursorPos(centerX, centerY);
    }

    private int GetNextIndex(int currentIndex, int screenCount, MovementMode movementMode)
    {
        if (movementMode == MovementMode.Loop || screenCount <= 2)
        {
            direction = 1;
            return (currentIndex + 1) % screenCount;
        }

        if (currentIndex <= 0)
        {
            direction = 1;
        }
        else if (currentIndex >= screenCount - 1)
        {
            direction = -1;
        }

        var nextIndex = currentIndex + direction;

        if (nextIndex >= screenCount - 1)
        {
            direction = -1;
        }
        else if (nextIndex <= 0)
        {
            direction = 1;
        }

        return nextIndex;
    }

    private int GetFallbackNextIndex(int screenCount, MovementMode movementMode)
    {
        if (movementMode == MovementMode.Loop || screenCount <= 2)
        {
            direction = 1;
            return (fallbackIndex + 1) % screenCount;
        }

        if (fallbackIndex < 0)
        {
            direction = 1;
            return 0;
        }

        return GetNextIndex(fallbackIndex, screenCount, movementMode);
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