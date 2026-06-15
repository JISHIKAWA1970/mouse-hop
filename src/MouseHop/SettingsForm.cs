namespace MouseHop;

internal sealed class SettingsForm : Form
{
    private readonly Label currentHotKeyLabel = new();
    private readonly Button changeButton = new();
    private readonly ComboBox movementModeComboBox = new();
    private bool waitingForHotKey;
    private bool leftWinDown;
    private bool rightWinDown;

    internal event EventHandler<HotKeySettings>? HotKeyChanged;
    internal event EventHandler<MovementMode>? MovementModeChanged;

    internal SettingsForm(AppSettings settings)
    {
        Text = "Mouse Hop 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 190);
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

        var movementModeLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 122),
            Text = "移動方式:"
        };

        movementModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        movementModeComboBox.Location = new Point(96, 118);
        movementModeComboBox.Width = 120;
        movementModeComboBox.Items.Add(new MovementModeItem("ループ", MovementMode.Loop));
        movementModeComboBox.Items.Add(new MovementModeItem("往復", MovementMode.PingPong));
        movementModeComboBox.SelectedIndexChanged += OnMovementModeSelectedIndexChanged;

        Controls.Add(descriptionLabel);
        Controls.Add(currentHotKeyLabel);
        Controls.Add(changeButton);
        Controls.Add(movementModeLabel);
        Controls.Add(movementModeComboBox);

        SetSettings(settings);
    }

    internal void SetSettings(AppSettings settings)
    {
        SetCurrentHotKey(settings.HotKey);
        SetMovementMode(settings.MovementMode);
    }

    internal void SetCurrentHotKey(HotKeySettings settings)
    {
        currentHotKeyLabel.Text = settings.DisplayText;
        if (!waitingForHotKey)
        {
            changeButton.Text = "変更";
        }
    }

    internal void SetMovementMode(MovementMode movementMode)
    {
        for (var i = 0; i < movementModeComboBox.Items.Count; i++)
        {
            if (movementModeComboBox.Items[i] is MovementModeItem item && item.Value == movementMode)
            {
                movementModeComboBox.SelectedIndex = i;
                return;
            }
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

    private void OnMovementModeSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (movementModeComboBox.SelectedItem is MovementModeItem item)
        {
            MovementModeChanged?.Invoke(this, item.Value);
        }
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

    private sealed record MovementModeItem(string Text, MovementMode Value)
    {
        public override string ToString() => Text;
    }
}