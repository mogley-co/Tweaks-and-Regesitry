using MogleyTweaks.Services;
using Microsoft.Win32;

namespace MogleyTweaks.Forms;

public sealed class PackReviewForm : Form
{
    private readonly RegistryPack _pack;
    private readonly RegistryStore _registry;
    private readonly BackupStore _backups;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly Button _apply = new() { Text = "I understand — apply pack", AutoSize = true };
    public PackReviewForm(RegistryPack pack, RegistryStore registry, BackupStore backups)
    {
        _pack = pack; _registry = registry; _backups = backups;
        Text = "Review Registry Pack";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(850, 560);
        MinimumSize = new Size(680, 420);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = pack.Name, AutoSize = true, Font = new Font("Segoe UI", 17, FontStyle.Bold) }, 0, 0);
        root.Controls.Add(new Label { Text = "⚠ Only HKEY_CURRENT_USER\\Software values are supported. Packs can still change app or Windows behavior. Never import packs from people you do not trust.\n" + pack.Description, AutoSize = true, MaximumSize = new Size(790, 0), ForeColor = Color.FromArgb(150, 82, 20), Padding = new Padding(0, 8, 0, 12) }, 0, 1);
        _grid.Columns.Add("path", "Registry location");
        _grid.Columns.Add("name", "Value");
        _grid.Columns.Add("type", "Type");
        _grid.Columns.Add("data", "New data");
        foreach (var e in pack.Entries)
        {
            var data = RegistryPackStore.GetValue(e);
            var display = data is byte[] bytes ? Convert.ToHexString(bytes) : data is string[] lines ? string.Join(" | ", lines) : Convert.ToString(data);
            _grid.Rows.Add("HKCU\\" + e.Key, string.IsNullOrEmpty(e.Name) ? "(Default)" : e.Name, e.Kind, display);
        }
        root.Controls.Add(_grid, 0, 2);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
        var cancel = new Button { Text = "Cancel", AutoSize = true };
        cancel.Click += (_, _) => Close();
        _apply.Click += ApplyPack;
        bottom.Controls.Add(cancel); bottom.Controls.Add(_apply);
        root.Controls.Add(bottom, 0, 3);
        Controls.Add(root);
    }

    private void ApplyPack(object? sender, EventArgs e)
    {
        var answer = MessageBox.Show(this,
            "This pack will change " + _pack.Entries.Count + " registry value(s). The creator may have included harmful or misleading changes. Backups will be saved first.\n\nDo you want to continue?",
            "Final safety check", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        _apply.Enabled = false;
        var backedUp = new List<(string Key, string Name)>();
        try
        {
            foreach (var entry in _pack.Entries)
                _backups.SaveValue(RegistryHive.CurrentUser, entry.Key, entry.Name);
            foreach (var entry in _pack.Entries)
            {
                var kind = Enum.Parse<RegistryValueKind>(entry.Kind, true);
                _registry.SetValue(RegistryHive.CurrentUser, entry.Key, entry.Name, RegistryPackStore.GetValue(entry), kind);
                backedUp.Add((entry.Key, entry.Name));
            }
            MessageBox.Show(this, "The pack was applied. Some changes may require restarting the app, File Explorer, or Windows.", "Pack applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "The pack stopped part-way through. Some values may already have changed. Backups were saved for all entries in the backup folder.\n\n" + ex.Message, "Pack partially applied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _apply.Enabled = true;
        }
    }
}
