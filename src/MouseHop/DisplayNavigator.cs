namespace MouseHop;

internal sealed class DisplayNavigator
{
    private int fallbackIndex = -1;
    private int direction = 1;

    internal bool MoveToNextDisplayCenter(MovementMode movementMode, IReadOnlyList<string> displayOrder)
    {
        var screens = GetOrderedScreens(displayOrder);
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

    internal static DisplayInfo[] GetCurrentDisplays(IReadOnlyList<string> displayOrder)
    {
        return GetOrderedScreens(displayOrder)
            .Select((screen, index) => new DisplayInfo(
                screen.DeviceName,
                $"{index + 1}. {screen.DeviceName} - {screen.Bounds.Width}x{screen.Bounds.Height} @ ({screen.Bounds.Left}, {screen.Bounds.Top}){(screen.Primary ? " / Primary" : string.Empty)}"))
            .ToArray();
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

    private static Screen[] GetOrderedScreens(IReadOnlyList<string> displayOrder)
    {
        var currentScreens = Screen.AllScreens;
        if (displayOrder.Count == 0)
        {
            return GetAutoOrderedScreens(currentScreens);
        }

        var remainingScreens = currentScreens.ToList();
        var orderedScreens = new List<Screen>();

        foreach (var deviceName in displayOrder)
        {
            var match = remainingScreens.FirstOrDefault(screen => string.Equals(screen.DeviceName, deviceName, StringComparison.Ordinal));
            if (match is null)
            {
                continue;
            }

            orderedScreens.Add(match);
            remainingScreens.Remove(match);
        }

        orderedScreens.AddRange(GetAutoOrderedScreens(remainingScreens));
        return orderedScreens.ToArray();
    }

    private static Screen[] GetAutoOrderedScreens(IEnumerable<Screen> screens)
    {
        return screens
            .OrderBy(screen => screen.Bounds.Top)
            .ThenBy(screen => screen.Bounds.Left)
            .ThenBy(screen => screen.DeviceName, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed record DisplayInfo(string DeviceName, string DisplayText)
{
    public override string ToString() => DisplayText;
}