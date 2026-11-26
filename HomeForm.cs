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
        MinimumSize = new Size(760, 600);
        Size = new Size(920, 700);
        Font = new Font("Segoe UI", 10);
        var dark = ThemeService.IsDark();
        BackColor = dark ? Color.FromArgb(27, 27, 31) : Color.FromArgb(246, 247, 250);
        ForeColor = dark ? Color.FromArgb(239, 239, 244) : Color.FromArgb(30, 31, 36);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label { Text = "Tweaks and Registry", AutoSize = true, Font = new Font("Segoe UI Semibold", 25, FontStyle.Bold), ForeColor = dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(40, 35, 70), Margin = new Padding(0, 0, 0, 2) };
        var subtitle = new Label { Text = "Make Windows feel more like yours.", AutoSize = true, ForeColor = dark ? Color.FromArgb(165, 165, 177) : Color.FromArgb(100, 101, 112), Margin = new Padding(2, 0, 0, 20) };
        root.Controls.Add(title, 0, 0);
        root.Controls.Add(subtitle, 0, 1);

        var warning = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = dark ? Color.FromArgb(69, 52, 27) : Color.FromArgb(255, 244, 220), Padding = new Padding(14), Margin = new Padding(0, 0, 0, 22) };
        warning.Controls.Add(new Label {
            Text = "⚠ Registry changes can damage Windows or apps if they are incorrect. Only apply tweaks and packs you understand. Backups are created before changes, but they cannot guarantee recovery.",
            AutoSize = true, MaximumSize = new Size(810, 0), ForeColor = dark ? Color.FromArgb(255, 218, 154) : Color.FromArgb(112, 72, 10)
        });
        root.Controls.Add(warning, 0, 2);

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 0, 0, 12) };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
        cards.Controls.Add(Card("Quick Tweaks", "50+ searchable switches for appearance, Explorer, Start, taskbar, privacy, gaming and more.", "Open Quick Tweaks", dark, (_, _) => OpenQuickTweaks()), 0, 0);
        cards.Controls.Add(Card("Registry Packs", "Import a .regimp pack, review every change, and apply it.", "Import .regimp", dark, (_, _) => ImportPack()), 1, 0);
        cards.Controls.Add(Card("Advanced Registry Editor", "Browse and edit registry keys and values manually.", "Open Editor", dark, (_, _) => OpenAdvanced()), 0, 1);
        cards.Controls.Add(Card("Backups", "View the registry value backups created by this app.", "Open Backup Folder", dark, (_, _) => OpenBackups()), 1, 1);
        cards.Controls.Add(Card("System Tools", "Inspect startup entries, installed apps, running processes, drives and network adapters. Open built-in Windows management tools.", "Open System Tools", dark, (_, _) => OpenSystemTools()), 0, 2);
        root.Controls.Add(cards, 0, 3);

        var footer = new Label { Text = "Windows 10 / 11  •  Local changes only  •  No account required", AutoSize = true, ForeColor = dark ? Color.FromArgb(145, 145, 158) : Color.FromArgb(125, 126, 136), Margin = new Padding(0, 10, 0, 0) };
        root.Controls.Add(footer, 0, 4);
        Controls.Add(root);
    }

    private static Control Card(string heading, string description, string action, bool dark, EventHandler click)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = dark ? Color.FromArgb(39, 39, 45) : Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 14, 14), BorderStyle = BorderStyle.FixedSingle };
        var title = new Label { Text = heading, AutoSize = true, Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold), ForeColor = dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(42, 38, 66), Location = new Point(18, 18) };
        var body = new Label { Text = description, AutoSize = true, MaximumSize = new Size(340, 0), ForeColor = dark ? Color.FromArgb(170, 170, 181) : Color.FromArgb(100, 101, 112), Location = new Point(18, 55) };
        var button = new Button { Text = action, AutoSize = true, Height = 36, Location = new Point(18, 112), FlatStyle = FlatStyle.Flat, BackColor = dark ? Color.FromArgb(130, 105, 238) : Color.FromArgb(104, 78, 210), ForeColor = Color.White, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        panel.Controls.Add(title);
        panel.Controls.Add(body);
        panel.Controls.Add(button);
        panel.Resize += (_, _) => button.Top = Math.Max(105, panel.ClientSize.Height - button.Height - 18);
        return panel;
    }

    private void OpenSystemTools()
    {
        using var form = new SystemToolsForm();
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
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", _backups.Folder) { UseShellExecute = true });
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
