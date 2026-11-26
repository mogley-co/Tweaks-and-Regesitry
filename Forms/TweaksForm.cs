using Microsoft.Win32;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class TweaksForm : Form
{
    private sealed record Tweak(
        string Group, string Title, string Description,
        string Path, string Name, int EnabledValue, int DisabledValue,
        bool EnabledWhenValueIsZero = false);

    private readonly RegistryStore _registry;
    private readonly BackupStore _backups;
    private readonly bool _dark = ThemeService.IsDark();
    private readonly Dictionary<Tweak, CheckBox> _checks = new();
    private readonly Label _status = new() { AutoSize = true };
    private Color _accent = Color.FromArgb(105, 75, 210);
    private Button? _accentButton;

    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
    private const string Dwm = @"Software\Microsoft\Windows\DWM";

    private readonly List<Tweak> _tweaks = new()
    {
        new("APPEARANCE", "Use dark mode", "Use dark colors for Windows and supported apps.", Personalize, "AppsUseLightTheme", 0, 1),
        new("APPEARANCE", "Turn off transparency", "Use solid Windows surfaces instead of transparency.", Personalize, "EnableTransparency", 0, 1),
        new("APPEARANCE", "Show accent color on taskbar and Start", "Use your Windows accent color on more surfaces.", Dwm, "ColorPrevalence", 1, 0),
        new("FILE EXPLORER", "Show file extensions", "Display endings such as .txt, .png and .zip.", ExplorerAdvanced, "HideFileExt", 0, 1),
        new("FILE EXPLORER", "Show hidden files", "Display files and folders marked as hidden.", ExplorerAdvanced, "Hidden", 1, 2),
        new("FILE EXPLORER", "Open to This PC", "Open File Explorer at your drives instead of Home.", ExplorerAdvanced, "LaunchTo", 1, 2),
        new("FILE EXPLORER", "Show full path in title bar", "Display the complete folder path in File Explorer.", ExplorerAdvanced, "ShowFullPath", 1, 0),
        new("FILE EXPLORER", "Show the status bar", "Display item information at the bottom of File Explorer.", ExplorerAdvanced, "ShowStatusBar", 1, 0),
        new("FILE EXPLORER", "Show image and video thumbnails", "Show previews instead of generic file icons.", ExplorerAdvanced, "IconsOnly", 0, 1),
        new("FILE EXPLORER", "Show drive letters first", "Place drive letters before drive names in This PC.", ExplorerAdvanced, "ShowDriveLettersFirst", 4, 0),
        new("FILE EXPLORER", "Show sync provider notifications", "Allow cloud storage apps to show sync messages in Explorer.", ExplorerAdvanced, "ShowSyncProviderNotifications", 1, 0),
        new("FILE EXPLORER", "Ask before deleting files", "Show a confirmation before sending a file to the Recycle Bin.", ExplorerAdvanced, "ConfirmFileDelete", 1, 0),
        new("FILE EXPLORER", "Use check boxes to select items", "Show selection check boxes beside files and folders.", ExplorerAdvanced, "AutoCheckSelect", 1, 0),
        new("FILE EXPLORER", "Open folder windows in separate processes", "Improves isolation, but can use more memory.", ExplorerAdvanced, "SeparateProcess", 1, 0),
        new("FILE EXPLORER HOME", "Show recently used files", "Show recent files in File Explorer Home / Quick Access.", ExplorerAdvanced, "ShowRecent", 1, 0),
        new("FILE EXPLORER HOME", "Show frequently used folders", "Show frequently visited folders in File Explorer Home.", ExplorerAdvanced, "ShowFrequent", 1, 0),
        new("TASKBAR", "Show seconds in the clock", "Display hours, minutes and seconds on the taskbar clock.", ExplorerAdvanced, "ShowSecondsInSystemClock", 1, 0),
        new("TASKBAR", "Align taskbar icons to the left", "Move taskbar icons to the left side (Windows 11).", ExplorerAdvanced, "TaskbarAl", 0, 1),
        new("TASKBAR", "Enable taskbar animations", "Use Windows animations for taskbar interactions.", ExplorerAdvanced, "TaskbarAnimations", 1, 0),
        new("TASKBAR", "Show Task View button", "Show the desktop and open-window overview button.", ExplorerAdvanced, "ShowTaskViewButton", 1, 0),
        new("TASKBAR", "Show Widgets button", "Show the Widgets entry point on the Windows 11 taskbar.", ExplorerAdvanced, "TaskbarDa", 1, 0),
        new("NOTIFICATION AREA", "Show all tray icons", "Keep notification-area icons visible instead of hiding some.", Explorer, "EnableAutoTray", 0, 1)
    };

    public TweaksForm(RegistryStore registry, BackupStore backups)
    {
        _registry = registry;
        _backups = backups;

        Text = "Quick Tweaks";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(790, 760);
        MinimumSize = new Size(680, 620);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = _dark ? Color.FromArgb(25, 25, 29) : Color.FromArgb(245, 246, 249);
        ForeColor = _dark ? Color.FromArgb(239, 239, 244) : Color.FromArgb(35, 35, 42);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 14),
            ColumnCount = 1, RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = "Quick Tweaks", AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(238, 231, 255) : Color.FromArgb(47, 39, 82),
            Margin = new Padding(0, 0, 0, 14)
        };
        root.Controls.Add(heading, 0, 0);

        var list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(0, 2, 8, 4),
            BackColor = BackColor
        };

        string? previousGroup = null;
        foreach (var tweak in _tweaks)
        {
            if (!IsWindows11() && (tweak.Name is "TaskbarAl" or "TaskbarDa"))
                continue;

            if (tweak.Group != previousGroup)
            {
                list.Controls.Add(MakeGroupHeader(tweak.Group));
                previousGroup = tweak.Group;
            }
            list.Controls.Add(MakeTweakRow(tweak));
        }

        var accentGroup = new FlowLayoutPanel
        {
            Width = 700, Height = 74, AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            Padding = new Padding(12, 10, 12, 8), Margin = new Padding(0, 8, 0, 12),
            BackColor = _dark ? Color.FromArgb(37, 37, 43) : Color.White
        };
        var accentText = new Label
        {
            Text = "ACCENT COLOR\nPick the highlight color used by Windows.",
            AutoSize = false, Width = 470, Height = 48,
            ForeColor = _dark ? Color.White : Color.FromArgb(35, 35, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _accentButton = new Button
        {
            Text = "Choose & apply", Width = 150, Height = 36,
            BackColor = Color.FromArgb(105, 78, 210), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 8, 0, 0)
        };
        _accentButton.FlatAppearance.BorderSize = 0;
        _accentButton.Click += ChooseAndApplyAccent;
        accentGroup.Controls.Add(accentText);
        accentGroup.Controls.Add(_accentButton);
        list.Controls.Add(accentGroup);

        root.Controls.Add(list, 0, 1);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(0, 10, 0, 0)
        };
        var warning = new Label
        {
            Text = "Each Apply button changes only that one setting. A backup is saved before its registry value changes.",
            AutoSize = true, MaximumSize = new Size(700, 0),
            ForeColor = _dark ? Color.FromArgb(255, 210, 145) : Color.FromArgb(133, 83, 20),
            Margin = new Padding(0, 0, 0, 6)
        };
        _status.ForeColor = _dark ? Color.FromArgb(180, 180, 190) : Color.FromArgb(90, 90, 100);
        var export = new Button
        {
            Text = "Export current setup (.regimp)", AutoSize = true, Height = 34,
            BackColor = Color.FromArgb(74, 112, 160), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 0)
        };
        export.FlatAppearance.BorderSize = 0;
        export.Click += ExportCurrentSetup;
        footer.Controls.Add(warning, 0, 0);
        footer.Controls.Add(_status, 0, 1);
        footer.Controls.Add(export, 0, 2);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        LoadCurrentValues();
        ApplyThemeTo(this);
        UpdateAccentButton();
    }

    private Control MakeGroupHeader(string title) => new Label
    {
        Text = title, AutoSize = true,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = _dark ? Color.FromArgb(174, 157, 232) : Color.FromArgb(105, 78, 170),
        Margin = new Padding(2, 12, 0, 6)
    };

    private Control MakeTweakRow(Tweak tweak)
    {
        var row = new Panel
        {
            Width = 700, Height = 66, Margin = new Padding(0, 0, 0, 7),
            BackColor = _dark ? Color.FromArgb(37, 37, 43) : Color.White
        };
        var check = new CheckBox
        {
            Text = tweak.Title, AutoSize = true, Location = new Point(12, 8),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(240, 240, 245) : Color.FromArgb(36, 36, 42)
        };
        var hint = new Label
        {
            Text = tweak.Description, AutoSize = true, MaximumSize = new Size(510, 0),
            Location = new Point(34, 34),
            ForeColor = _dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(108, 108, 118),
            Font = new Font("Segoe UI", 8.3f)
        };
        var apply = new Button
        {
            Text = "Apply", Width = 76, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(610, 17), BackColor = Color.FromArgb(105, 78, 210),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        apply.FlatAppearance.BorderSize = 0;
        apply.Click += (_, _) => ApplyOne(tweak, check.Checked);

        _checks[tweak] = check;
        row.Controls.Add(check);
        row.Controls.Add(hint);
        row.Controls.Add(apply);
        return row;
    }

    private static bool IsWindows11() => Environment.OSVersion.Version.Build >= 22000;

    private void LoadCurrentValues()
    {
        foreach (var tweak in _tweaks)
        {
            if ((tweak.Name is "TaskbarAl" or "TaskbarDa") && !IsWindows11()) continue;
            var current = ReadDword(tweak.Path, tweak.Name, tweak.DisabledValue);
            _checks[tweak].Checked = current == tweak.EnabledValue;
        }

        var dark = ReadDword(Personalize, "AppsUseLightTheme", 1) == 0 &&
                   ReadDword(Personalize, "SystemUsesLightTheme", 1) == 0;
        _checks[_tweaks[0]].Checked = dark;
        _accent = Color.FromArgb(ReadDword(Dwm, "ColorizationColor", unchecked((int)0xFF694BD2)));
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

    private void ApplyOne(Tweak tweak, bool enabled)
    {
        if (tweak.Name is "TaskbarAl" or "TaskbarDa" && !IsWindows11())
        {
            MessageBox.Show(this, "This option is only available on Windows 11.", "Not supported",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this,
            $"Apply only this setting?\n\n{tweak.Title}\n\nA backup will be saved before changing it.",
            "Apply one tweak", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            if (tweak.Name == "AppsUseLightTheme")
            {
                var value = enabled ? 0 : 1;
                SetDword(Personalize, "AppsUseLightTheme", value);
                SetDword(Personalize, "SystemUsesLightTheme", value);
            }
            else
            {
                SetDword(tweak.Path, tweak.Name, enabled ? tweak.EnabledValue : tweak.DisabledValue);
            }

            _status.Text = $"Applied: {tweak.Title}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not apply setting",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "That setting could not be applied.";
        }
    }

    private void SetDword(string path, string name, int value)
    {
        var current = ReadDword(path, name, int.MinValue);
        if (current == value) return;
        _backups.SaveValue(RegistryHive.CurrentUser, path, name);
        _registry.SetValue(RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord);
    }

    private void ChooseAndApplyAccent(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = _accent, FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _accent = dialog.Color;
        if (MessageBox.Show(this, "Apply this accent color to Windows?", "Apply accent color",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            SetDword(Dwm, "ColorizationColor", _accent.ToArgb());
            SetDword(Dwm, "ColorPrevalence", 1);
            UpdateAccentButton();
            _status.Text = "Applied: Accent color";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not apply accent color",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateAccentButton()
    {
        if (_accentButton is null) return;
        _accentButton.BackColor = _accent;
        _accentButton.ForeColor = _accent.GetBrightness() < 0.55f ? Color.White : Color.Black;
        _accentButton.Text = $"Apply #{_accent.R:X2}{_accent.G:X2}{_accent.B:X2}";
    }


    private void ExportCurrentSetup(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export Quick Tweaks setup",
            Filter = "Mogley Registry Pack (*.regimp)|*.regimp",
            DefaultExt = "regimp",
            AddExtension = true,
            FileName = "Mogley-QuickTweaks.regimp",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var namesByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var tweak in _tweaks)
            {
                if (!namesByPath.TryGetValue(tweak.Path, out var names))
                    namesByPath[tweak.Path] = names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                names.Add(tweak.Name);
            }

            // Include both halves of the dark-mode preference and the chosen accent color.
            namesByPath[Personalize].Add("SystemUsesLightTheme");
            namesByPath[Dwm].Add("ColorizationColor");

            var values = new List<(string KeyPath, RegistryValueItem Value)>();
            foreach (var group in namesByPath)
            {
                foreach (var value in _registry.GetValues(RegistryHive.CurrentUser, group.Key))
                {
                    if (group.Value.Contains(value.Name))
                        values.Add((group.Key, value));
                }
            }

            RegistryPackStore.ExportMany(dialog.FileName, "Quick Tweaks setup",
                "Exported Windows customization settings from Mogley Tweaks.", values);

            _status.Text = $"Exported {values.Count} registry value(s).";
            MessageBox.Show(this,
                $"Your Quick Tweaks setup was exported.\\n\\n{values.Count} value(s) saved to:\\n{dialog.FileName}\\n\\nReview the pack before importing it on another PC.",
                "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not export pack",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Export failed.";
        }
    }

    private void ApplyThemeTo(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Panel)
                control.BackColor = _dark ? Color.FromArgb(37, 37, 43) : Color.White;
            if (control is not Button && control is not CheckBox && control is not Label)
                control.ForeColor = _dark ? Color.FromArgb(238, 238, 243) : Color.FromArgb(35, 35, 42);
            ApplyThemeTo(control);
        }
    }
}
