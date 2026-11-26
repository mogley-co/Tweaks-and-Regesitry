using Microsoft.Win32;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class TweaksForm : Form
{
    private readonly RegistryStore _registry;
    private readonly BackupStore _backups;
    private readonly Button _colorButton = new() { Text = "Choose accent color...", AutoSize = true };
    private Color _accent = Color.FromArgb(105, 75, 210);
    private readonly ComboBox _appTheme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly ComboBox _systemTheme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly CheckBox _extensions = new() { Text = "Show file extensions", AutoSize = true };
    private readonly CheckBox _hidden = new() { Text = "Show hidden files", AutoSize = true };
    private readonly CheckBox _thisPc = new() { Text = "Open File Explorer to This PC", AutoSize = true };
    private readonly CheckBox _leftTaskbar = new() { Text = "Align taskbar to the left", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    private readonly bool _dark = ThemeService.IsDark();

    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Dwm = @"Software\Microsoft\Windows\DWM";

    public TweaksForm(RegistryStore registry, BackupStore backups)
    {
        _registry = registry;
        _backups = backups;
        Text = "Quick Tweaks";
        StartPosition = FormStartPosition.CenterParent;
        Width = 760;
        Height = 760;
        MinimumSize = new Size(620, 620);
        BackColor = _dark ? Color.FromArgb(27, 27, 31) : Color.FromArgb(246, 247, 250);
        ForeColor = _dark ? Color.FromArgb(238, 238, 243) : Color.FromArgb(35, 35, 42);
        Font = new Font("Segoe UI", 10);

        _appTheme.Items.AddRange(new object[] { "Light", "Dark" });
        _systemTheme.Items.AddRange(new object[] { "Light", "Dark" });
        _colorButton.Click += ChooseAccent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(22),
            ColumnCount = 1, RowCount = 5, AutoScroll = true
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Quick Tweaks",
            AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(47, 39, 82),
            Margin = new Padding(0, 0, 0, 3)
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = "Choose what you want to change, then apply once.",
            AutoSize = true,
            ForeColor = _dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(105, 105, 115),
            Margin = new Padding(0, 0, 0, 18)
        }, 0, 1);

        var appearance = MakeGroup("Appearance");
        var appearanceFlow = MakeFlow();
        appearanceFlow.Controls.Add(Field("App appearance", _appTheme, "Changes the theme used by apps that follow Windows app mode."));
        appearanceFlow.Controls.Add(Field("Windows appearance", _systemTheme, "Changes the Windows light/dark preference."));
        appearanceFlow.Controls.Add(Field("Accent color", _colorButton, "Changes the Windows accent color."));
        appearance.Controls.Add(appearanceFlow);
        root.Controls.Add(appearance, 0, 2);

        var explorer = MakeGroup("File Explorer");
        var explorerFlow = MakeFlow(true);
        explorerFlow.Controls.Add(WithHelp(_extensions, "Show file extensions such as .txt and .png."));
        explorerFlow.Controls.Add(WithHelp(_hidden, "Show files and folders marked as hidden."));
        explorerFlow.Controls.Add(WithHelp(_thisPc, "Open File Explorer at This PC instead of Quick Access/Home."));
        explorer.Controls.Add(explorerFlow);
        root.Controls.Add(explorer, 0, 3);

        var taskbar = MakeGroup("Taskbar");
        var taskbarFlow = MakeFlow(true);
        taskbarFlow.Controls.Add(WithHelp(_leftTaskbar, "Windows 11 only. Moves taskbar icons to the left."));
        _leftTaskbar.Visible = IsWindows11();
        taskbar.Controls.Add(taskbarFlow);
        root.Controls.Add(taskbar, 0, 4);

        var footer = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 105, ColumnCount = 2, Padding = new Padding(18, 10, 18, 12) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var warning = new Label
        {
            Text = "Changes are written to the Windows registry. A backup is saved before each changed value, but backups are not a full system restore. Some changes need Explorer restart or sign-out.",
            AutoSize = true, MaximumSize = new Size(490, 0),
            ForeColor = _dark ? Color.FromArgb(255, 213, 145) : Color.FromArgb(128, 82, 20),
            Padding = new Padding(0, 8, 8, 0)
        };
        var apply = new Button { Text = "Apply changes", AutoSize = true, Height = 40, BackColor = Color.FromArgb(105, 78, 210), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Right };
        apply.FlatAppearance.BorderSize = 0;
        apply.Click += ApplyChanges;
        footer.Controls.Add(warning, 0, 0);
        footer.Controls.Add(apply, 1, 0);
        footer.Controls.Add(_status, 0, 1);
        Controls.Add(root);
        Controls.Add(footer);

        LoadCurrentValues();
        ApplyThemeTo(this);
    }

    private GroupBox MakeGroup(string title) => new()
    {
        Text = title, Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(14), Margin = new Padding(0, 0, 0, 14),
        ForeColor = _dark ? Color.FromArgb(226, 221, 246) : Color.FromArgb(55, 48, 88)
    };

    private static FlowLayoutPanel MakeFlow(bool vertical = false) => new()
    {
        Dock = DockStyle.Top, AutoSize = true, WrapContents = true,
        FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight,
        Padding = new Padding(4), Margin = new Padding(0)
    };

    private Control Field(string heading, Control control, string help)
    {
        var panel = new Panel { Width = 215, Height = 104, Margin = new Padding(4, 6, 12, 6) };
        var title = new Label { Text = heading, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(0, 0) };
        control.Location = new Point(0, 26);
        var description = new Label { Text = help, AutoSize = true, MaximumSize = new Size(205, 0), ForeColor = _dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(105, 105, 115), Location = new Point(0, 60) };
        panel.Controls.Add(title);
        panel.Controls.Add(control);
        panel.Controls.Add(description);
        return panel;
    }

    private Control WithHelp(CheckBox check, string help)
    {
        var panel = new Panel { Width = 620, Height = 54, Margin = new Padding(3, 5, 3, 5) };
        check.Location = new Point(0, 0);
        var description = new Label { Text = help, AutoSize = true, ForeColor = _dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(105, 105, 115), Location = new Point(24, 25) };
        panel.Controls.Add(check);
        panel.Controls.Add(description);
        return panel;
    }

    private static bool IsWindows11() => Environment.OSVersion.Version.Build >= 22000;

    private void LoadCurrentValues()
    {
        _appTheme.SelectedIndex = ReadDword(Personalize, "AppsUseLightTheme", 1) == 0 ? 1 : 0;
        _systemTheme.SelectedIndex = ReadDword(Personalize, "SystemUsesLightTheme", 1) == 0 ? 1 : 0;
        _extensions.Checked = ReadDword(ExplorerAdvanced, "HideFileExt", 1) == 0;
        _hidden.Checked = ReadDword(ExplorerAdvanced, "Hidden", 2) == 1;
        _thisPc.Checked = ReadDword(ExplorerAdvanced, "LaunchTo", 2) == 1;
        _leftTaskbar.Checked = ReadDword(ExplorerAdvanced, "TaskbarAl", 1) == 0;
        _accent = Color.FromArgb(ReadDword(Dwm, "ColorizationColor", unchecked((int)0xFF694BD2)));
        UpdateColorButton();
    }

    private int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using var key = _registry.OpenRead(Microsoft.Win32.RegistryHive.CurrentUser, path);
            return Convert.ToInt32(key.GetValue(name, fallback));
        }
        catch { return fallback; }
    }

    private void ChooseAccent(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = _accent, FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _accent = dialog.Color;
            UpdateColorButton();
        }
    }

    private void UpdateColorButton()
    {
        _colorButton.BackColor = _accent;
        _colorButton.ForeColor = _accent.GetBrightness() < 0.55f ? Color.White : Color.Black;
        _colorButton.Text = $"Accent: #{_accent.R:X2}{_accent.G:X2}{_accent.B:X2}";
    }

    private void ApplyChanges(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this,
            "Apply the selected Windows settings? Registry values will be changed and backups will be saved first.",
            "Confirm changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            SetIfChanged(Personalize, "AppsUseLightTheme", _appTheme.SelectedIndex == 0 ? 1 : 0);
            SetIfChanged(Personalize, "SystemUsesLightTheme", _systemTheme.SelectedIndex == 0 ? 1 : 0);
            SetIfChanged(ExplorerAdvanced, "HideFileExt", _extensions.Checked ? 0 : 1);
            SetIfChanged(ExplorerAdvanced, "Hidden", _hidden.Checked ? 1 : 2);
            SetIfChanged(ExplorerAdvanced, "LaunchTo", _thisPc.Checked ? 1 : 2);

            if (IsWindows11())
                SetIfChanged(ExplorerAdvanced, "TaskbarAl", _leftTaskbar.Checked ? 0 : 1);

            SetIfChanged(Dwm, "ColorizationColor", _accent.ToArgb());
            SetIfChanged(Dwm, "ColorPrevalence", 1);

            _status.Text = "Applied. Some changes may need Explorer restart or sign-out.";
            MessageBox.Show(this,
                "Your selected settings were applied. Some changes need File Explorer to restart or Windows to sign out before they appear.",
                "Tweaks applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not apply settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Apply failed.";
        }
    }

    private void SetIfChanged(string path, string name, int value)
    {
        var current = ReadDword(path, name, int.MinValue);
        if (current == value) return;
        _backups.SaveValue(Microsoft.Win32.RegistryHive.CurrentUser, path, name);
        _registry.SetValue(Microsoft.Win32.RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord);
    }

    private void ApplyThemeTo(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is GroupBox)
                control.BackColor = _dark ? Color.FromArgb(39, 39, 45) : Color.White;
            else if (control is Panel)
                control.BackColor = _dark ? Color.FromArgb(39, 39, 45) : Color.White;
            else if (control is TableLayoutPanel)
                control.BackColor = _dark ? Color.FromArgb(27, 27, 31) : Color.FromArgb(246, 247, 250);
            else if (control is not Button)
                control.BackColor = _dark ? Color.FromArgb(39, 39, 45) : Color.White;

            if (control is not Button && control is not CheckBox)
                control.ForeColor = _dark ? Color.FromArgb(238, 238, 243) : Color.FromArgb(35, 35, 42);
            ApplyThemeTo(control);
        }
    }
}
