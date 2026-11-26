using System.Diagnostics;
using MogleyTweaks.Forms;
using MogleyTweaks.Services;

namespace MogleyTweaks;

public sealed class HomeForm : Form
{
    private readonly RegistryStore _registry = new();
    private readonly BackupStore _backups = new();

    public HomeForm()
    {
        Text = "Tweaks and Registry";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 620);
        Size = new Size(1040, 760);
        Font = new Font("Segoe UI", 9.5f);
        var dark = ThemeService.IsDark();
        BackColor = dark ? Color.FromArgb(25, 25, 29) : Color.FromArgb(245, 246, 249);
        ForeColor = dark ? Color.FromArgb(239, 239, 244) : Color.FromArgb(30, 31, 36);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 16),
            ColumnCount = 1, RowCount = 4, BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = "Tweaks and Registry", AutoSize = true,
            Font = new Font("Segoe UI Semibold", 25, FontStyle.Bold),
            ForeColor = dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(40, 35, 70),
            Margin = new Padding(0, 0, 0, 2)
        };
        var subtitle = new Label
        {
            Text = "Make Windows feel more like yours.", AutoSize = true,
            ForeColor = dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(100, 101, 112),
            Margin = new Padding(2, 0, 0, 16)
        };
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(subtitle, 0, 1);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = BackColor, Margin = Padding.Empty
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var warning = new Panel
        {
            Dock = DockStyle.Top, Height = 72, Padding = new Padding(14),
            BackColor = dark ? Color.FromArgb(69, 52, 27) : Color.FromArgb(255, 244, 220),
            Margin = new Padding(0, 0, 0, 14)
        };
        warning.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, AutoSize = false,
            Text = "Registry changes can affect Windows or installed apps. Only apply changes you understand. Backups are created before edits, but cannot guarantee recovery.",
            ForeColor = dark ? Color.FromArgb(255, 218, 154) : Color.FromArgb(112, 72, 10),
            TextAlign = ContentAlignment.MiddleLeft
        });
        content.Controls.Add(warning, 0, 0);

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
            BackColor = BackColor, Margin = Padding.Empty, Padding = Padding.Empty
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 1));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 1));
        cards.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        cards.Controls.Add(Card("Quick Tweaks", "Search and apply Windows appearance, Explorer, taskbar, privacy and gaming settings.", "Open Quick Tweaks", dark, (_, _) => OpenQuickTweaks()), 0, 0);
        cards.Controls.Add(Card("Registry Packs", "Import a .regimp pack, review every proposed change, and apply it safely.", "Import .regimp", dark, (_, _) => ImportPack()), 1, 0);
        cards.Controls.Add(Card("Advanced Registry Editor", "Browse registry hives and manage keys and values manually.", "Open Editor", dark, (_, _) => OpenAdvanced()), 0, 1);
        cards.Controls.Add(Card("Backups", "Open and manage the registry value backups created by this app.", "Open Backup Folder", dark, (_, _) => OpenBackups()), 1, 1);
        cards.Controls.Add(Card("System Tools", "Inspect system information, startup entries, installed apps, processes, storage and network adapters.", "Open System Tools", dark, (_, _) => OpenSystemTools()), 0, 2);
        cards.Controls.Add(Card("Personalization", "Set desktop wallpaper, customize folder icons, and add or remove a File Explorer right-click action.", "Open Personalization", dark, (_, _) => OpenPersonalization()), 1, 2);
        content.Controls.Add(cards, 0, 1);
        root.Controls.Add(content, 0, 2);

        var footer = new Label
        {
            Text = "Windows 10 / 11   •   Local changes only   •   No account required",
            AutoSize = true,
            ForeColor = dark ? Color.FromArgb(145, 145, 158) : Color.FromArgb(125, 126, 136),
            Margin = new Padding(0, 12, 0, 0)
        };
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);
    }

    private static Control Card(string heading, string description, string action, bool dark, EventHandler click)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = dark ? Color.FromArgb(39, 39, 45) : Color.White,
            Padding = new Padding(16), Margin = new Padding(0, 0, 12, 12),
            BorderStyle = BorderStyle.FixedSingle, MinimumSize = new Size(0, 108)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Margin = Padding.Empty, BackColor = panel.BackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = heading, AutoSize = true, Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold),
            ForeColor = dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(42, 38, 66),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = description, AutoSize = false, Dock = DockStyle.Fill,
            ForeColor = dark ? Color.FromArgb(170, 170, 181) : Color.FromArgb(100, 101, 112),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);
        var button = new Button
        {
            Text = action, AutoSize = true, Height = 32, Anchor = AnchorStyles.Left,
            FlatStyle = FlatStyle.Flat, BackColor = dark ? Color.FromArgb(130, 105, 238) : Color.FromArgb(104, 78, 210),
            ForeColor = Color.White, Cursor = Cursors.Hand, Margin = Padding.Empty, Padding = new Padding(8, 2, 8, 2)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        layout.Controls.Add(button, 0, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private void OpenSystemTools()
    {
        using var form = new SystemToolsForm();
        form.ShowDialog(this);
    }

    private void OpenPersonalization()
    {
        using var form = new PersonalizationForm();
        form.ShowDialog(this);
    }

    private void OpenQuickTweaks()
    {
        using var form = new TweaksForm(_registry, _backups);
        form.ShowDialog(this);
    }

    private void OpenAdvanced()
    {
        using var form = new MainForm();
        form.ShowDialog(this);
    }

    private void OpenBackups()
    {
        Directory.CreateDirectory(_backups.Folder);
        Process.Start(new ProcessStartInfo("explorer.exe", _backups.Folder) { UseShellExecute = true });
    }

    private void ImportPack()
    {
        using var dialog = new OpenFileDialog { Filter = "Mogley Registry Packs (*.regimp)|*.regimp", Title = "Choose a registry pack" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var pack = RegistryPackStore.Load(dialog.FileName);
            using var review = new PackReviewForm(pack, _registry, _backups);
            review.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "This pack could not be opened. It may be invalid or unsafe.\n\n" + ex.Message, "Pack rejected", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
