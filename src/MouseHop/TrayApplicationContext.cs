namespace MouseHop;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly DisplayNavigator displayNavigator = new();
    private readonly HotKeyWindow hotKeyWindow = new();
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem pauseMenuItem;
    private bool paused;

    internal TrayApplicationContext()
    {
        pauseMenuItem = new ToolStripMenuItem("一時停止", null, OnPauseClicked)
        {
            CheckOnClick = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("次のディスプレイへ移動", null, OnMoveNextClicked);
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
        hotKeyWindow.RegisterMoveHotKey();
    }

    private void OnHotKeyPressed(object? sender, EventArgs e)
    {
        MoveNextIfEnabled();
    }

    private void OnMoveNextClicked(object? sender, EventArgs e)
    {
        MoveNextIfEnabled();
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
            hotKeyWindow.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
