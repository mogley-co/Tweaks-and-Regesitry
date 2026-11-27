using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class PersonalizationForm : Form
{
    private readonly BackupStore _backups = new();
    private readonly bool _dark = ThemeService.IsDark();
    private readonly Label _status = new() { AutoSize = true };

    private const string WallpaperValuePath = @"Control Panel\Desktop";

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, string value, uint flags);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    public PersonalizationForm()
    {
        Text = "Windows Personalization";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(620, 460);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = _dark ? Color.FromArgb(25, 25, 29) : Color.FromArgb(245, 246, 249);
        ForeColor = _dark ? Color.FromArgb(239, 239, 244) : Color.FromArgb(35, 35, 42);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 4,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Windows Personalization", AutoSize = true,
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(238, 231, 255) : Color.FromArgb(47, 39, 82),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = "Wallpaper, folder icons and a useful File Explorer context-menu shortcut.",
            AutoSize = true, ForeColor = _dark ? Color.Silver : Color.DimGray,
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 1);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight, BackColor = BackColor, Padding = new Padding(0, 2, 8, 8)
        };
        cards.Controls.Add(MakeCard("Desktop wallpaper",
            "Choose an image file and set it as your Windows desktop background. The previous wallpaper registry value is backed up.",
            "Choose wallpaper…", ChooseWallpaper));
        cards.Controls.Add(MakeCard("Folder icon",
            "Choose a folder and an .ico file. Windows uses a desktop.ini file for that folder; an existing desktop.ini is copied to the app backup folder first.",
            "Change folder icon…", ChangeFolderIcon));
        cards.Controls.Add(MakeCard("Windows desktop icons",
            "Open Windows Themes settings to show or hide This PC, Recycle Bin, Network and your user folder, and customize their system icons.",
            "Open desktop icon settings", OpenDesktopIconSettings));
        root.Controls.Add(cards, 0, 2);

        _status.ForeColor = _dark ? Color.FromArgb(180, 180, 190) : Color.FromArgb(90, 90, 100);
        _status.Margin = new Padding(0, 12, 0, 0);
        root.Controls.Add(_status, 0, 3);
        Controls.Add(root);
    }

    private Control MakeCard(string title, string description, string action, EventHandler click)
    {
        var panel = new Panel
        {
            Width = 330, Height = 188, Margin = new Padding(0, 0, 12, 12), Padding = new Padding(14),
            BackColor = _dark ? Color.FromArgb(39, 39, 45) : Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        var heading = new Label
        {
            Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(42, 38, 66),
            Location = new Point(14, 12)
        };
        var body = new Label
        {
            Text = description, AutoSize = false, Location = new Point(14, 42),
            Size = new Size(294, 88), ForeColor = _dark ? Color.FromArgb(175, 175, 187) : Color.FromArgb(100, 101, 112)
        };
        var button = new Button
        {
            Text = action, AutoSize = true, Location = new Point(14, 139), Height = 32,
            BackColor = Color.FromArgb(105, 78, 210), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        panel.Controls.Add(heading);
        panel.Controls.Add(body);
        panel.Controls.Add(button);
        return panel;
    }

    private void OpenDesktopIconSettings(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:themes") { UseShellExecute = true });
            _status.Text = "Opened Windows Themes settings. Select Desktop icon settings there.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open Themes settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChooseWallpaper(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a desktop wallpaper",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _backups.SaveValue(RegistryHive.CurrentUser, WallpaperValuePath, "WallPaper");
            if (!SystemParametersInfo(20, 0, dialog.FileName, 0x01 | 0x02))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            _status.Text = "Desktop wallpaper updated. Previous wallpaper registry value backed up.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not change wallpaper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Wallpaper change failed.";
        }
    }

    private void ChangeFolderIcon(object? sender, EventArgs e)
    {
        using var folder = new FolderBrowserDialog { Description = "Choose the folder whose icon you want to change." };
        if (folder.ShowDialog(this) != DialogResult.OK) return;
        using var icon = new OpenFileDialog
        {
            Title = "Choose an icon file",
            Filter = "Icon files (*.ico)|*.ico",
            CheckFileExists = true
        };
        if (icon.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var ini = Path.Combine(folder.SelectedPath, "desktop.ini");
            Directory.CreateDirectory(_backups.Folder);
            if (File.Exists(ini))
            {
                var backup = Path.Combine(_backups.Folder, $"desktop-ini-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.bak");
                File.Copy(ini, backup, overwrite: false);
            }

            File.WriteAllText(ini, $"[.ShellClassInfo]{Environment.NewLine}IconResource={icon.FileName},0{Environment.NewLine}",
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.SetAttributes(ini, FileAttributes.Hidden | FileAttributes.System);
            var attributes = File.GetAttributes(folder.SelectedPath);
            File.SetAttributes(folder.SelectedPath, attributes | FileAttributes.System);
            SHChangeNotify(0x08000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
            _status.Text = "Folder icon updated. Any previous desktop.ini was copied to the app backup folder.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not change folder icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Folder icon change failed.";
        }
    }


}
