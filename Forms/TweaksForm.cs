using Microsoft.Win32;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class TweaksForm : Form
{
    private readonly RegistryStore _registry;
    private readonly BackupStore _backups;
    private readonly bool _dark = ThemeService.IsDark();

    private readonly CheckBox _darkMode = new() { Text = "Dark mode", AutoSize = true };
    private readonly CheckBox _extensions = new() { Text = "Show file extensions", AutoSize = true };
    private readonly CheckBox _hidden = new() { Text = "Show hidden files", AutoSize = true };
    private readonly CheckBox _thisPc = new() { Text = "Open File Explorer to This PC", AutoSize = true };
    private readonly CheckBox _seconds = new() { Text = "Show seconds in the taskbar clock", AutoSize = true };
    private readonly CheckBox _transparency = new() { Text = "Turn off transparency effects", AutoSize = true };
    private readonly CheckBox _leftTaskbar = new() { Text = "Align taskbar icons to the left", AutoSize = true };
    private readonly Button _accentButton = new() { Text = "Choose accent color", AutoSize = true, Height = 34 };
    private readonly Label _status = new() { AutoSize = true };

    private Color _accent = Color.FromArgb(105, 75, 210);

    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Dwm = @"Software\Microsoft\Windows\DWM";

    public TweaksForm(RegistryStore registry, BackupStore backups)
    {
        _registry = registry;
        _backups = backups;

        Text = "Quick Tweaks";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 690);
        MinimumSize = new Size(620, 600);
        Font = new Font("Segoe UI", 10);
        BackColor = _dark ? Color.FromArgb(25, 25, 29) : Color.FromArgb(245, 246, 249);
        ForeColor = _dark ? Color.FromArgb(239, 239, 244) : Color.FromArgb(35, 35, 42);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 16),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = "Quick Tweaks",
            AutoSize = true,
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(238, 231, 255) : Color.FromArgb(47, 39, 82),
            Margin = new Padding(0, 0, 0, 4)
        };
        var subtitle = new Label
        {
            Text = "Simple Windows settings. Turn on what you want, then press Apply.",
            AutoSize = true,
            ForeColor = _dark ? Color.FromArgb(170, 170, 181) : Color.FromArgb(105, 105, 115),
            Margin = new Padding(0, 0, 0, 16)
        };
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(subtitle, 0, 1);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 2, 8, 4),
            BackColor = BackColor
        };

        var appearance = MakeSection("LOOK & FEEL");
        appearance.Controls.Add(MakeOption(_darkMode, "Use dark colors across Windows and supported apps."));
        appearance.Controls.Add(MakeOption(_transparency, "Turn off transparent surfaces in Windows."));
        appearance.Controls.Add(MakeOption(_accentButton, "Pick the color used for Windows highlights."));
        options.Controls.Add(appearance);

        var explorer = MakeSection("FILE EXPLORER");
        explorer.Controls.Add(MakeOption(_extensions, "See endings like .txt, .png and .zip."));
        explorer.Controls.Add(MakeOption(_hidden, "Show files and folders marked as hidden."));
        explorer.Controls.Add(MakeOption(_thisPc, "Open to your drives instead of Home or Quick Access."));
        options.Controls.Add(explorer);

        var taskbar = MakeSection("TASKBAR");
        taskbar.Controls.Add(MakeOption(_seconds, "Display hours, minutes and seconds."));
        if (IsWindows11())
            taskbar.Controls.Add(MakeOption(_leftTaskbar, "Move taskbar icons to the left side."));
        options.Controls.Add(taskbar);

        root.Controls.Add(options, 0, 2);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 12, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var warning = new Label
        {
            Text = "Registry changes can affect Windows. Backups are saved before changes, but they are not a full system restore.",
            AutoSize = true,
            MaximumSize = new Size(455, 0),
            ForeColor = _dark ? Color.FromArgb(255, 210, 145) : Color.FromArgb(133, 83, 20),
            Padding = new Padding(0, 7, 8, 0)
        };
        var apply = new Button
        {
            Text = "Apply changes",
            AutoSize = true,
            Height = 40,
            BackColor = Color.FromArgb(105, 78, 210),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8, 0, 0, 0)
        };
        apply.FlatAppearance.BorderSize = 0;
        apply.Click += ApplyChanges;

        footer.Controls.Add(warning, 0, 0);
        footer.Controls.Add(apply, 1, 0);
        footer.Controls.Add(_status, 0, 1);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);

        _accentButton.Click += ChooseAccent;
        LoadCurrentValues();
        ApplyThemeTo(this);
        _accentButton.BackColor = _accent;
        _accentButton.ForeColor = _accent.GetBrightness() < 0.55f ? Color.White : Color.Black;
    }

    private Panel MakeSection(string title)
    {
        var section = new Panel
        {
            Width = 630,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = _dark ? Color.FromArgb(37, 37, 43) : Color.White,
            Padding = new Padding(15, 12, 15, 12),
            Margin = new Padding(0, 0, 0, 14)
        };
        var label = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(174, 157, 232) : Color.FromArgb(105, 78, 170),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 8)
        };
        section.Controls.Add(label);
        return section;
    }

    private Control MakeOption(Control control, string description)
    {
        var row = new Panel
        {
            Width = 590,
            Height = 59,
            Margin = new Padding(0, 5, 0, 5),
            BackColor = _dark ? Color.FromArgb(44, 44, 51) : Color.FromArgb(249, 249, 252)
        };
        control.Location = new Point(12, 8);
        if (control is CheckBox check)
        {
            check.ForeColor = _dark ? Color.FromArgb(240, 240, 245) : Color.FromArgb(36, 36, 42);
            check.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        }
        var hint = new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(550, 0),
            Location = new Point(34, 32),
            ForeColor = _dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(108, 108, 118),
            Font = new Font("Segoe UI", 8.5f)
        };
        row.Controls.Add(control);
        row.Controls.Add(hint);
        return row;
    }

    private static bool IsWindows11() => Environment.OSVersion.Version.Build >= 22000;

    private void LoadCurrentValues()
    {
        _darkMode.Checked =
            ReadDword(Personalize, "AppsUseLightTheme", 1) == 0 &&
            ReadDword(Personalize, "SystemUsesLightTheme", 1) == 0;

        _extensions.Checked = ReadDword(ExplorerAdvanced, "HideFileExt", 1) == 0;
        _hidden.Checked = ReadDword(ExplorerAdvanced, "Hidden", 2) == 1;
        _thisPc.Checked = ReadDword(ExplorerAdvanced, "LaunchTo", 2) == 1;
        _seconds.Checked = ReadDword(ExplorerAdvanced, "ShowSecondsInSystemClock", 0) == 1;
        _transparency.Checked = ReadDword(Personalize, "EnableTransparency", 1) == 0;
        _leftTaskbar.Checked = ReadDword(ExplorerAdvanced, "TaskbarAl", 1) == 0;
        _accent = Color.FromArgb(ReadDword(Dwm, "ColorizationColor", unchecked((int)0xFF694BD2)));
        _accentButton.BackColor = _accent;
        _accentButton.ForeColor = _accent.GetBrightness() < 0.55f ? Color.White : Color.Black;
        _accentButton.Text = $"Accent color  •  #{_accent.R:X2}{_accent.G:X2}{_accent.B:X2}";
    }

    private int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using var key = _registry.OpenRead(RegistryHive.CurrentUser, path);
            return Convert.ToInt32(key.GetValue(name, fallback));
        }
        catch { return fallback; }
    }

    private void ChooseAccent(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = _accent, FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _accent = dialog.Color;
        _accentButton.BackColor = _accent;
        _accentButton.ForeColor = _accent.GetBrightness() < 0.55f ? Color.White : Color.Black;
        _accentButton.Text = $"Accent color  •  #{_accent.R:X2}{_accent.G:X2}{_accent.B:X2}";
    }

    private void ApplyChanges(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this,
            "Apply the selected settings? Backups will be created for values that change.",
            "Apply Windows tweaks", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            var dark = _darkMode.Checked ? 0 : 1;
            SetIfChanged(Personalize, "AppsUseLightTheme", dark);
            SetIfChanged(Personalize, "SystemUsesLightTheme", dark);
            SetIfChanged(Personalize, "EnableTransparency", _transparency.Checked ? 0 : 1);

            SetIfChanged(ExplorerAdvanced, "HideFileExt", _extensions.Checked ? 0 : 1);
            SetIfChanged(ExplorerAdvanced, "Hidden", _hidden.Checked ? 1 : 2);
            SetIfChanged(ExplorerAdvanced, "LaunchTo", _thisPc.Checked ? 1 : 2);
            SetIfChanged(ExplorerAdvanced, "ShowSecondsInSystemClock", _seconds.Checked ? 1 : 0);

            if (IsWindows11())
                SetIfChanged(ExplorerAdvanced, "TaskbarAl", _leftTaskbar.Checked ? 0 : 1);

            SetIfChanged(Dwm, "ColorizationColor", _accent.ToArgb());
            SetIfChanged(Dwm, "ColorPrevalence", 1);

            _status.Text = "Settings applied.";
            MessageBox.Show(this,
                "Your settings were applied. Some changes may need File Explorer to restart or Windows to sign out.",
                "All done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _status.Text = "Some settings could not be applied.";
            MessageBox.Show(this, ex.Message, "Could not apply settings",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetIfChanged(string path, string name, int value)
    {
        var current = ReadDword(path, name, int.MinValue);
        if (current == value) return;

        _backups.SaveValue(RegistryHive.CurrentUser, path, name);
        _registry.SetValue(RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord);
    }

    private void ApplyThemeTo(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Panel)
                control.BackColor = control.Parent == null || control.Parent == this
                    ? (_dark ? Color.FromArgb(37, 37, 43) : Color.White)
                    : (_dark ? Color.FromArgb(44, 44, 51) : Color.FromArgb(249, 249, 252));

            if (control is not Button && control is not CheckBox && control is not Label)
                control.ForeColor = _dark ? Color.FromArgb(238, 238, 243) : Color.FromArgb(35, 35, 42);

            ApplyThemeTo(control);
        }
    }
}
