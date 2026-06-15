namespace MouseHop;

internal sealed class SettingsForm : Form
{
    private readonly Label currentHotKeyLabel = new();
    private readonly Button changeButton = new();
    private bool waitingForHotKey;
    private bool leftWinDown;
    private bool rightWinDown;

    internal event EventHandler<HotKeySettings>? HotKeyChanged;

    internal SettingsForm(HotKeySettings currentHotKey)
    {
        Text = "Mouse Hop 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 130);
        KeyPreview = true;

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 16),
            Text = "現在のホットキー:"
        };

        currentHotKeyLabel.AutoSize = true;
        currentHotKeyLabel.Location = new Point(16, 44);
        currentHotKeyLabel.Font = new Font(currentHotKeyLabel.Font, FontStyle.Bold);

        changeButton.AutoSize = true;
        changeButton.Location = new Point(16, 80);
        changeButton.Text = "変更";
        changeButton.Click += OnChangeClicked;

        Controls.Add(descriptionLabel);
        Controls.Add(currentHotKeyLabel);
        Controls.Add(changeButton);

        SetCurrentHotKey(currentHotKey);
    }

    internal void SetCurrentHotKey(HotKeySettings settings)
    {
        currentHotKeyLabel.Text = settings.DisplayText;
        if (!waitingForHotKey)
        {
            changeButton.Text = "変更";
        }
    }

    private void OnChangeClicked(object? sender, EventArgs e)
    {
        waitingForHotKey = true;
        changeButton.Text = "キー入力待ち...";
        currentHotKeyLabel.Text = "新しいホットキーを押してください";
        ActiveControl = null;
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.LWin)
        {
            leftWinDown = true;
        }
        else if (e.KeyCode == Keys.RWin)
        {
            rightWinDown = true;
        }

        if (waitingForHotKey && TryCaptureHotKey(e.KeyCode, e.Modifiers))
        {
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.LWin)
        {
            leftWinDown = false;
        }
        else if (e.KeyCode == Keys.RWin)
        {
            rightWinDown = false;
        }

        base.OnKeyUp(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!waitingForHotKey)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        var key = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        return TryCaptureHotKey(key, modifiers) || base.ProcessCmdKey(ref msg, keyData);
    }

    private bool TryCaptureHotKey(Keys key, Keys keyModifiers)
    {
        if (key is Keys.None or Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return false;
        }

        var modifiers = 0u;
        if ((keyModifiers & Keys.Control) == Keys.Control)
        {
            modifiers |= NativeMethods.ModControl;
        }

        if ((keyModifiers & Keys.Alt) == Keys.Alt)
        {
            modifiers |= NativeMethods.ModAlt;
        }

        if ((keyModifiers & Keys.Shift) == Keys.Shift)
        {
            modifiers |= NativeMethods.ModShift;
        }

        if (leftWinDown || rightWinDown)
        {
            modifiers |= NativeMethods.ModWin;
        }

        var settings = new HotKeySettings(modifiers, key);
        waitingForHotKey = false;
        changeButton.Text = "変更";
        SetCurrentHotKey(settings);
        HotKeyChanged?.Invoke(this, settings);
        return true;
    }
}
