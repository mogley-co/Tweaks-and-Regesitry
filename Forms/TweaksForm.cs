using Microsoft.Win32;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class TweaksForm : Form
{
    private readonly RegistryStore _registry;
    private readonly BackupStore _backups;
    private readonly Button _colorButton = new() { Text = "Choose accent color...", AutoSize = true };
    private Color _accent = Color.FromArgb(105, 75, 210);
    private readonly ComboBox _appTheme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox _systemTheme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly CheckBox _extensions = new() { Text = "Show file extensions", AutoSize = true };
    private readonly CheckBox _hidden = new() { Text = "Show hidden files", AutoSize = true };
    private readonly CheckBox _thisPc = new() { Text = "Open File Explorer to This PC", AutoSize = true };
    private readonly CheckBox _leftTaskbar = new() { Text = "Align taskbar to the left (Windows 11)", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };

    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Dwm = @"Software\Microsoft\Windows\DWM";

    public TweaksForm(RegistryStore registry, BackupStore backups)
    {
        _registry = registry;
        _backups = backups;
        Text = "Windows Tweaks";
        StartPosition = FormStartPosition.CenterParent;
        Width = 650; Height = 650; MinimumSize = new Size(560, 540);

        _appTheme.Items.AddRange(new object[] { "Light", "Dark" });
        _systemTheme.Items.AddRange(new object[] { "Light", "Dark" });
        _colorButton.Click += ChooseAccent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(18),
            ColumnCount = 1, RowCount = 7, AutoScroll = true
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(Section("Appearance"), 0, 0);
        var appearance = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        appearance.Controls.Add(new Label { Text = "Windows accent", AutoSize = true, Padding = new Padding(0, 8, 8, 0) });
        appearance.Controls.Add(_colorButton);
        appearance.Controls.Add(new Label { Text = "App theme", AutoSize = true, Padding = new Padding(12, 8, 4, 0) });
        appearance.Controls.Add(_appTheme);
        appearance.Controls.Add(new Label { Text = "System theme", AutoSize = true, Padding = new Padding(12, 8, 4, 0) });
        appearance.Controls.Add(_systemTheme);
        layout.Controls.Add(appearance, 0, 1);

        layout.Controls.Add(Section("File Explorer"), 0, 2);
        var explorer = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        explorer.Controls.Add(_extensions);
        explorer.Controls.Add(_hidden);
        explorer.Controls.Add(_thisPc);
        layout.Controls.Add(explorer, 0, 3);

        layout.Controls.Add(Section("Taskbar"), 0, 4);
        var taskbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        taskbar.Controls.Add(_leftTaskbar);
        layout.Controls.Add(taskbar, 0, 5);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(12), WrapContents = false };
        var apply = new Button { Text = "Apply selected settings", AutoSize = true };
        apply.Click += ApplyChanges;
        bottom.Controls.Add(apply);
        bottom.Controls.Add(_status);
        Controls.Add(layout);
        Controls.Add(bottom);

        LoadCurrentValues();
        _leftTaskbar.Visible = IsWindows11();
        layout.Controls.Add(new Label
        {
            Text = "Note: Start menu size/layout controls are intentionally omitted. Microsoft has changed these settings between Windows releases, and there is no reliable shared registry switch for all supported Windows 10/11 builds.",
            AutoSize = true, MaximumSize = new Size(570, 0), Padding = new Padding(0, 14, 0, 0)
        }, 0, 6);
    }

    private static Label Section(string title) => new()
    {
        Text = title, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold),
        Padding = new Padding(0, 14, 0, 8)
    };

    private static bool IsWindows11() => Environment.OSVersion.Version.Build >= 22000;

    private void LoadCurrentValues()
    {
        _appTheme.SelectedIndex = ReadDword(Personalize, "AppsUseLightTheme", 1) == 0 ? 1 : 0;
        _systemTheme.SelectedIndex = ReadDword(Personalize, "SystemUsesLightTheme", 1) == 0 ? 1 : 0;
        _extensions.Checked = ReadDword(ExplorerAdvanced, "HideFileExt", 1) == 0;
        _hidden.Checked = ReadDword(ExplorerAdvanced, "Hidden", 2) == 1;
        _thisPc.Checked = ReadDword(ExplorerAdvanced, "LaunchTo", 2) == 1;
        _leftTaskbar.Checked = ReadDword(ExplorerAdvanced, "TaskbarAl", 1) == 0;
        var raw = ReadDword(Dwm, "ColorizationColor", unchecked((int)0xFF694BD2));
        _accent = Color.FromArgb(raw);
        UpdateColorButton();
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
        try
        {
            Set(Personalize, "AppsUseLightTheme", _appTheme.SelectedIndex == 0 ? 1 : 0);
            Set(Personalize, "SystemUsesLightTheme", _systemTheme.SelectedIndex == 0 ? 1 : 0);
            Set(ExplorerAdvanced, "HideFileExt", _extensions.Checked ? 0 : 1);
            Set(ExplorerAdvanced, "Hidden", _hidden.Checked ? 1 : 2);
            Set(ExplorerAdvanced, "LaunchTo", _thisPc.Checked ? 1 : 2);

            if (IsWindows11())
                Set(ExplorerAdvanced, "TaskbarAl", _leftTaskbar.Checked ? 0 : 1);

            Set(Dwm, "ColorizationColor", _accent.ToArgb());
            Set(Dwm, "ColorPrevalence", 1);

            _status.Text = "Applied. Some changes need Explorer restart or sign-out.";
            MessageBox.Show(this,
                "Settings were written. Some Windows components may need Explorer restart or sign-out before changes appear. The app theme updates the next time this utility starts.",
                "Tweaks applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not apply settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Apply failed.";
        }
    }

    private void Set(string path, string name, int value)
    {
        _backups.SaveValue(RegistryHive.CurrentUser, path, name);
        _registry.SetValue(RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord);
    }
}
