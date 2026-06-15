namespace MouseHop;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly DisplayNavigator displayNavigator = new();
    private readonly HotKeyWindow hotKeyWindow = new();
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem pauseMenuItem;
    private SettingsForm? settingsForm;
    private AppSettings settings;
    private bool paused;

    internal TrayApplicationContext()
    {
        settings = SettingsStore.Load();

        pauseMenuItem = new ToolStripMenuItem("一時停止", null, OnPauseClicked)
        {
            CheckOnClick = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("次のディスプレイへ移動", null, OnMoveNextClicked);
        menu.Items.Add("設定", null, OnSettingsClicked);
        menu.Items.Add(pauseMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, OnExitClicked);

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = LoadTrayIcon(),
            Text = "Mouse Hop",
            Visible = true
        };

        hotKeyWindow.HotKeyPressed += OnHotKeyPressed;
        RegisterCurrentHotKey();
    }

    private void RegisterCurrentHotKey()
    {
        if (!hotKeyWindow.TryRegisterMoveHotKey(settings.HotKey, out var errorMessage))
        {
            notifyIcon.ShowBalloonTip(
                5000,
                "Mouse Hop",
                errorMessage ?? $"{settings.HotKey.DisplayText} の登録に失敗しました。",
                ToolTipIcon.Warning);
        }
    }

    private void OnHotKeyPressed(object? sender, EventArgs e)
    {
        MoveNextIfEnabled();
    }

    private void OnMoveNextClicked(object? sender, EventArgs e)
    {
        MoveNextIfEnabled();
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (settingsForm is null || settingsForm.IsDisposed)
        {
            settingsForm = new SettingsForm(settings);
            settingsForm.HotKeyChanged += OnSettingsHotKeyChanged;
            settingsForm.MovementModeChanged += OnSettingsMovementModeChanged;
            settingsForm.DisplayOrderChanged += OnSettingsDisplayOrderChanged;
            settingsForm.FormClosed += (_, _) => settingsForm = null;
        }

        settingsForm.Show();
        settingsForm.Activate();
    }

    private void OnSettingsHotKeyChanged(object? sender, HotKeySettings hotKey)
    {
        settings = settings with { HotKey = hotKey };
        SettingsStore.Save(settings);
        RegisterCurrentHotKey();
        settingsForm?.SetCurrentHotKey(settings.HotKey);
    }

    private void OnSettingsMovementModeChanged(object? sender, MovementMode movementMode)
    {
        settings = settings with { MovementMode = movementMode };
        SettingsStore.Save(settings);
        settingsForm?.SetMovementMode(settings.MovementMode);
    }

    private void OnSettingsDisplayOrderChanged(object? sender, IReadOnlyList<string> displayOrder)
    {
        settings = settings with { DisplayOrder = displayOrder.ToArray() };
        SettingsStore.Save(settings);
        settingsForm?.SetDisplayOrder(settings.DisplayOrder);
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        paused = pauseMenuItem.Checked;
        pauseMenuItem.Text = paused ? "再開" : "一時停止";
    }

    private void MoveNextIfEnabled()
    {
        if (paused)
        {
            return;
        }

        displayNavigator.MoveToNextDisplayCenter(settings.MovementMode, settings.DisplayOrder);
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            return CreateGeneratedTrayIcon();
        }
        catch
        {
            // Fall back to the default icon if the generated icon cannot be created.
            return SystemIcons.Application;
        }
    }

    private static Icon CreateGeneratedTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var monitorBrush = new SolidBrush(Color.FromArgb(37, 99, 235));
            using var screenBrush = new SolidBrush(Color.FromArgb(219, 234, 254));
            using var hopPen = new Pen(Color.FromArgb(34, 197, 94), 3);
            using var hopBrush = new SolidBrush(Color.FromArgb(34, 197, 94));
            using var cursorBrush = new SolidBrush(Color.White);
            using var cursorPen = new Pen(Color.FromArgb(15, 23, 42), 1.5f);

            graphics.FillRectangle(monitorBrush, 2, 11, 11, 11);
            graphics.FillRectangle(screenBrush, 4, 13, 7, 6);
            graphics.FillRectangle(monitorBrush, 19, 11, 11, 11);
            graphics.FillRectangle(screenBrush, 21, 13, 7, 6);

            graphics.DrawArc(hopPen, 8, 4, 16, 14, 205, 130);
            graphics.FillEllipse(hopBrush, 22, 7, 5, 5);

            var cursorPoints = new[]
            {
                new Point(10, 5),
                new Point(10, 26),
                new Point(15, 21),
                new Point(19, 29),
                new Point(23, 27),
                new Point(19, 19),
                new Point(26, 19)
            };

            graphics.FillPolygon(cursorBrush, cursorPoints);
            graphics.DrawPolygon(cursorPen, cursorPoints);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            settingsForm?.Dispose();
            hotKeyWindow.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
