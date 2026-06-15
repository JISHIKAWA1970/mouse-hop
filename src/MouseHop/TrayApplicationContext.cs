namespace MouseHop;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly DisplayNavigator displayNavigator = new();
    private readonly HotKeyWindow hotKeyWindow = new();
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem pauseMenuItem;
    private SettingsForm? settingsForm;
    private HotKeySettings currentHotKey;
    private bool paused;

    internal TrayApplicationContext()
    {
        currentHotKey = SettingsStore.Load();

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
            Icon = SystemIcons.Application,
            Text = "Mouse Hop",
            Visible = true
        };

        hotKeyWindow.HotKeyPressed += OnHotKeyPressed;
        RegisterCurrentHotKey();
    }

    private void RegisterCurrentHotKey()
    {
        if (!hotKeyWindow.TryRegisterMoveHotKey(currentHotKey, out var errorMessage))
        {
            notifyIcon.ShowBalloonTip(
                5000,
                "Mouse Hop",
                errorMessage ?? $"{currentHotKey.DisplayText} の登録に失敗しました。",
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
            settingsForm = new SettingsForm(currentHotKey);
            settingsForm.HotKeyChanged += OnSettingsHotKeyChanged;
            settingsForm.FormClosed += (_, _) => settingsForm = null;
        }

        settingsForm.Show();
        settingsForm.Activate();
    }

    private void OnSettingsHotKeyChanged(object? sender, HotKeySettings settings)
    {
        currentHotKey = settings;
        SettingsStore.Save(currentHotKey);
        RegisterCurrentHotKey();
        settingsForm?.SetCurrentHotKey(currentHotKey);
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

        displayNavigator.MoveToNextDisplayCenter();
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