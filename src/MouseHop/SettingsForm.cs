namespace MouseHop;

internal sealed class SettingsForm : Form
{
    private readonly Label currentHotKeyLabel = new();
    private readonly Button changeButton = new();
    private readonly ComboBox movementModeComboBox = new();
    private readonly ListBox displayOrderListBox = new();
    private readonly Button moveUpButton = new();
    private readonly Button moveDownButton = new();
    private readonly CheckBox startWithWindowsCheckBox = new();
    private readonly Label installationStatusLabel = new();
    private readonly Label installationPathLabel = new();
    private readonly Button installButton = new();
    private bool updatingStartWithWindows;
    private bool waitingForHotKey;
    private bool leftWinDown;
    private bool rightWinDown;

    internal event EventHandler<HotKeySettings>? HotKeyChanged;
    internal event EventHandler<MovementMode>? MovementModeChanged;
    internal event EventHandler<IReadOnlyList<string>>? DisplayOrderChanged;
    internal event EventHandler<bool>? StartWithWindowsChanged;
    internal event EventHandler? InstallToStandardLocationRequested;

    internal SettingsForm(AppSettings settings)
    {
        Text = "Mouse Hop 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 620);
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

        var displayOrderLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 166),
            Text = "ディスプレイ順:"
        };

        displayOrderListBox.Location = new Point(16, 194);
        displayOrderListBox.Size = new Size(420, 130);

        moveUpButton.AutoSize = true;
        moveUpButton.Location = new Point(452, 194);
        moveUpButton.Text = "上へ";
        moveUpButton.Click += (_, _) => MoveSelectedDisplay(-1);

        moveDownButton.AutoSize = true;
        moveDownButton.Location = new Point(452, 232);
        moveDownButton.Text = "下へ";
        moveDownButton.Click += (_, _) => MoveSelectedDisplay(1);

        var displayOrderHelpLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 334),
            Size = new Size(560, 44),
            Text = "接続中のディスプレイだけを表示します。未接続の保存済みディスプレイは無視し、新しく増えたディスプレイは末尾に追加します。"
        };

        startWithWindowsCheckBox.AutoSize = true;
        startWithWindowsCheckBox.Location = new Point(16, 392);
        startWithWindowsCheckBox.Text = "Windows 起動時に Mouse Hop を起動する";
        startWithWindowsCheckBox.CheckedChanged += OnStartWithWindowsCheckedChanged;

        var installationLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 434),
            Text = "標準配置:"
        };

        installationStatusLabel.AutoSize = true;
        installationStatusLabel.Location = new Point(96, 434);
        installationStatusLabel.Font = new Font(installationStatusLabel.Font, FontStyle.Bold);

        installationPathLabel.AutoSize = false;
        installationPathLabel.Location = new Point(16, 462);
        installationPathLabel.Size = new Size(580, 70);

        installButton.AutoSize = true;
        installButton.Location = new Point(16, 542);
        installButton.Text = "標準フォルダへ配置";
        installButton.Click += (_, _) => InstallToStandardLocationRequested?.Invoke(this, EventArgs.Empty);

        Controls.Add(displayOrderLabel);
        Controls.Add(displayOrderListBox);
        Controls.Add(moveUpButton);
        Controls.Add(moveDownButton);
        Controls.Add(displayOrderHelpLabel);
        Controls.Add(startWithWindowsCheckBox);
        Controls.Add(installationLabel);
        Controls.Add(installationStatusLabel);
        Controls.Add(installationPathLabel);
        Controls.Add(installButton);

        SetSettings(settings);
    }

    internal void SetSettings(AppSettings settings)
    {
        SetCurrentHotKey(settings.HotKey);
        SetMovementMode(settings.MovementMode);
        SetDisplayOrder(settings.DisplayOrder);
        SetStartWithWindows(settings.StartWithWindows);
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


    internal void SetDisplayOrder(IReadOnlyList<string> displayOrder)
    {
        var selectedDeviceName = (displayOrderListBox.SelectedItem as DisplayInfo)?.DeviceName;
        displayOrderListBox.Items.Clear();

        foreach (var display in DisplayNavigator.GetCurrentDisplays(displayOrder))
        {
            displayOrderListBox.Items.Add(display);
            if (string.Equals(display.DeviceName, selectedDeviceName, StringComparison.Ordinal))
            {
                displayOrderListBox.SelectedItem = display;
            }
        }

        if (displayOrderListBox.SelectedIndex < 0 && displayOrderListBox.Items.Count > 0)
        {
            displayOrderListBox.SelectedIndex = 0;
        }
    }

    private void MoveSelectedDisplay(int offset)
    {
        var currentIndex = displayOrderListBox.SelectedIndex;
        if (currentIndex < 0)
        {
            return;
        }

        var newIndex = currentIndex + offset;
        if (newIndex < 0 || newIndex >= displayOrderListBox.Items.Count)
        {
            return;
        }

        var item = displayOrderListBox.Items[currentIndex];
        displayOrderListBox.Items.RemoveAt(currentIndex);
        displayOrderListBox.Items.Insert(newIndex, item);
        displayOrderListBox.SelectedIndex = newIndex;

        var displayOrder = displayOrderListBox.Items
            .OfType<DisplayInfo>()
            .Select(display => display.DeviceName)
            .ToArray();
        DisplayOrderChanged?.Invoke(this, displayOrder);
    }


    internal void SetStartWithWindows(bool startWithWindows)
    {
        updatingStartWithWindows = true;
        try
        {
            startWithWindowsCheckBox.Checked = startWithWindows;
        }
        finally
        {
            updatingStartWithWindows = false;
        }
    }


    internal void SetInstallationStatus(InstallationStatus status)
    {
        installationStatusLabel.Text = status.IsInstalled
            ? "標準フォルダに配置済み"
            : "標準フォルダに未配置";

        var currentPath = string.IsNullOrWhiteSpace(status.CurrentExecutablePath)
            ? "取得できません"
            : status.CurrentExecutablePath;
        installationPathLabel.Text = $"現在の場所: {currentPath}{Environment.NewLine}標準の場所: {status.StandardExecutablePath}{Environment.NewLine}この場所へ配置して常用できます。";

        installButton.Enabled = !status.IsInstalled && status.CanInstall;
        installButton.Text = status.IsInstalled ? "標準フォルダに配置済み" : "標準フォルダへ配置";
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


    private void OnStartWithWindowsCheckedChanged(object? sender, EventArgs e)
    {
        if (updatingStartWithWindows)
        {
            return;
        }

        StartWithWindowsChanged?.Invoke(this, startWithWindowsCheckBox.Checked);
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
