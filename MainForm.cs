using Microsoft.Win32;
using MogleyTweaks.Forms;
using MogleyTweaks.Services;

namespace MogleyTweaks;

public sealed class MainForm : Form
{
    private readonly RegistryStore _registry = new();
    private readonly BackupStore _backups = new();
    private readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false, BorderStyle = BorderStyle.None };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, BorderStyle = BorderStyle.None
    };
    private readonly TextBox _search = new() { Width = 240, PlaceholderText = "Filter current key..." };
    private readonly Label _path = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(9, 7, 8, 0), AutoEllipsis = true };
    private readonly ToolStripStatusLabel _status = new("Ready");
    private RegistryHive _currentHive = RegistryHive.CurrentUser;
    private string _currentPath = "";
    private RegistryValueItem[] _currentValues = Array.Empty<RegistryValueItem>();
    private AppColors _colors = AppColors.Light;

    private sealed record TreeItem(RegistryHive Hive, string Path);
    private sealed record AppColors(Color Back, Color Surface, Color Text, Color Muted, Color Border, Color Accent, Color Input)
    {
        public static AppColors Light => new(
            Color.FromArgb(247, 247, 249), Color.White, Color.FromArgb(34, 34, 38),
            Color.FromArgb(105, 105, 113), Color.FromArgb(224, 224, 230),
            Color.FromArgb(105, 75, 210), Color.FromArgb(250, 250, 252));
        public static AppColors Dark => new(
            Color.FromArgb(27, 27, 30), Color.FromArgb(37, 37, 41), Color.FromArgb(238, 238, 242),
            Color.FromArgb(165, 165, 174), Color.FromArgb(62, 62, 69),
            Color.FromArgb(151, 113, 255), Color.FromArgb(46, 46, 51));
    }

    public MainForm()
    {
        Text = "Tweaks and Registry — Registry Editor";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1180; Height = 760; MinimumSize = new Size(850, 560);
        _colors = ThemeService.IsDark() ? AppColors.Dark : AppColors.Light;

        _grid.Columns.Add("name", "Name");
        _grid.Columns.Add("type", "Type");
        _grid.Columns.Add("data", "Data");

        var menu = BuildMenu();
        var toolbar = BuildToolbar();
        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
        Shown += (_, _) => split.SplitterDistance = Math.Min(340, split.Width - split.Panel2MinSize);
        split.Panel1.Controls.Add(_tree);
        split.Panel2.Controls.Add(_grid);

        var searchBar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6) };
        searchBar.Items.Add(new ToolStripLabel("Current key:"));
        var searchHost = new ToolStripControlHost(_search) { AutoSize = false, Width = 250 };
        searchBar.Items.Add(searchHost);
        var status = new StatusStrip();
        status.Items.Add(_status);

        Controls.Add(split);
        Controls.Add(_path);
        Controls.Add(searchBar);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(status);
        MainMenuStrip = menu;

        _tree.BeforeExpand += TreeBeforeExpand;
        _tree.AfterSelect += (_, _) => LoadSelectedKey();
        _grid.CellDoubleClick += (_, _) => EditSelectedValue();
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) { DeleteSelectedValue(); e.Handled = true; }
            if (e.KeyCode == Keys.Enter) { EditSelectedValue(); e.Handled = true; }
        };
        _search.TextChanged += (_, _) => RenderValues();

        LoadRoots();
        ApplyTheme();
    }

    protected override void WndProc(ref Message m)
    {
        const int WmSettingChange = 0x001A;
        base.WndProc(ref m);
        if (m.Msg == WmSettingChange)
        {
            _colors = ThemeService.IsDark() ? AppColors.Dark : AppColors.Light;
            ApplyTheme();
        }
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("Refresh", null, (_, _) => LoadSelectedKey());
        file.DropDownItems.Add("Export selected key as .reg...", null, (_, _) => ExportCurrentKey());
        file.DropDownItems.Add("Share selected key as .regimp...", null, (_, _) => ExportRegistryPack());
        file.DropDownItems.Add("Restore value backup...", null, (_, _) => RestoreBackup());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("Edit");
        edit.DropDownItems.Add("New key...", null, (_, _) => CreateKey());
        edit.DropDownItems.Add("New value...", null, (_, _) => CreateValue());
        edit.DropDownItems.Add("Edit value...", null, (_, _) => EditSelectedValue());
        edit.DropDownItems.Add("Delete value", null, (_, _) => DeleteSelectedValue());
        edit.DropDownItems.Add("Delete empty key", null, (_, _) => DeleteSelectedKey());

        var tools = new ToolStripMenuItem("Tools");
        tools.DropDownItems.Add("Windows Tweaks...", null, (_, _) => OpenTweaks());
        tools.DropDownItems.Add("Open backup folder", null, (_, _) => OpenBackupFolder());

        menu.Items.AddRange(new ToolStripItem[] { file, edit, tools });
        return menu;
    }

    private ToolStrip BuildToolbar()
    {
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(5) };
        Add("Refresh", LoadSelectedKey);
        Add("New key", CreateKey);
        Add("New value", CreateValue);
        Add("Edit", EditSelectedValue);
        Add("Delete value", DeleteSelectedValue);
        Add("Tweaks", OpenTweaks);
        return bar;

        void Add(string label, Action action)
        {
            var button = new ToolStripButton(label);
            button.Click += (_, _) => action();
            bar.Items.Add(button);
        }
    }

    private void LoadRoots()
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        foreach (var root in RegistryStore.Roots)
        {
            var node = new TreeNode(root.Name) { Tag = new TreeItem(root.Hive, "") };
            node.Nodes.Add(new TreeNode("Loading..."));
            _tree.Nodes.Add(node);
        }
        _tree.EndUpdate();
        _tree.SelectedNode = _tree.Nodes[1];
        _tree.Nodes[1].Expand();
    }

    private void TreeBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            PopulateChildren(e.Node);
    }

    private void PopulateChildren(TreeNode node)
    {
        if (node.Tag is not TreeItem item) return;
        node.Nodes.Clear();
        try
        {
            foreach (var child in _registry.GetSubKeys(item.Hive, item.Path))
            {
                var path = RegistryStore.Join(item.Path, child);
                var childNode = new TreeNode(child) { Tag = new TreeItem(item.Hive, path) };
                childNode.Nodes.Add(new TreeNode("Loading..."));
                node.Nodes.Add(childNode);
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Cannot read subkeys: " + ex.Message;
        }
    }

    private void LoadSelectedKey()
    {
        if (_tree.SelectedNode?.Tag is not TreeItem item) return;
        _currentHive = item.Hive;
        _currentPath = item.Path;
        _path.Text = RegistryStore.HiveName(_currentHive) +
            (string.IsNullOrEmpty(_currentPath) ? "" : "\\" + _currentPath);
        try
        {
            _currentValues = _registry.GetValues(_currentHive, _currentPath);
            RenderValues();
            _status.Text = $"{_currentValues.Length} value(s)";
        }
        catch (Exception ex)
        {
            _currentValues = Array.Empty<RegistryValueItem>();
            RenderValues();
            _status.Text = ex.Message;
        }
    }

    private void RenderValues()
    {
        var filter = _search.Text.Trim();
        _grid.Rows.Clear();
        foreach (var item in _currentValues)
        {
            if (filter.Length > 0 &&
                !item.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !item.DisplayData.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            _grid.Rows.Add(item.DisplayName, item.Kind.ToString(), item.DisplayData);
        }
    }

    private void CreateKey()
    {
        if (_tree.SelectedNode?.Tag is not TreeItem item) return;
        var name = Prompt("Enter the new key name:", "Create registry key");
        if (name is null) return;
        if (!Confirm($"Create this key?\n\n{RegistryStore.HiveName(item.Hive)}\\{RegistryStore.Join(item.Path, name)}"))
            return;
        try
        {
            _registry.CreateKey(item.Hive, item.Path, name);
            PopulateChildren(_tree.SelectedNode);
            _status.Text = "Key created.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void CreateValue()
    {
        using var dialog = new ValueEditorDialog("", "", RegistryValueKind.String, creating: true);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!Confirm($"Create value '{(dialog.ValueName.Length == 0 ? "(Default)" : dialog.ValueName)}' in:\n{CurrentFullPath()}"))
            return;
        try
        {
            _backups.SaveValue(_currentHive, _currentPath, dialog.ValueName);
            _registry.SetValue(_currentHive, _currentPath, dialog.ValueName, dialog.ParsedData, dialog.ValueKind);
            LoadSelectedKey();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void EditSelectedValue()
    {
        var selected = GetSelectedValue();
        if (selected is null) return;
        using var dialog = new ValueEditorDialog(selected.Name, selected.Data, selected.Kind, creating: false);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!Confirm($"Modify value '{selected.DisplayName}' in:\n{CurrentFullPath()}"))
            return;
        try
        {
            _backups.SaveValue(_currentHive, _currentPath, selected.Name);
            _registry.SetValue(_currentHive, _currentPath, selected.Name, dialog.ParsedData, dialog.ValueKind);
            LoadSelectedKey();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void DeleteSelectedValue()
    {
        var selected = GetSelectedValue();
        if (selected is null) return;
        if (!Confirm($"Delete value '{selected.DisplayName}'?\n\nA backup will be saved first."))
            return;
        try
        {
            _backups.SaveValue(_currentHive, _currentPath, selected.Name);
            _registry.DeleteValue(_currentHive, _currentPath, selected.Name);
            LoadSelectedKey();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private RegistryValueItem? GetSelectedValue()
    {
        if (_grid.CurrentRow is null) return null;
        var displayedName = Convert.ToString(_grid.CurrentRow.Cells[0].Value) ?? "";
        var name = displayedName == "(Default)" ? "" : displayedName;
        return _currentValues.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private void DeleteSelectedKey()
    {
        if (_tree.SelectedNode?.Parent?.Tag is not TreeItem parent ||
            _tree.SelectedNode.Tag is not TreeItem selected) return;
        var name = selected.Path.Split('\\').Last();
        if (!Confirm($"Delete this key only if it is empty?\n\n{CurrentFullPath()}"))
            return;
        try
        {
            _registry.DeleteEmptyKey(parent.Hive, parent.Path, name);
            var removed = _tree.SelectedNode;
            _tree.SelectedNode = removed.Parent;
            removed.Remove();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "The key could not be deleted. It may contain subkeys, or Windows may deny access.\n\n" + ex.Message,
                "Delete key failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportCurrentKey()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Registry files (*.reg)|*.reg",
            FileName = "registry-export.reg",
            DefaultExt = "reg"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var lines = new List<string> { "Windows Registry Editor Version 5.00", "", "[" + CurrentFullPath() + "]" };
            foreach (var item in _currentValues)
                lines.Add(FormatRegValue(item));
            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), System.Text.Encoding.Unicode);
            _status.Text = "Exported " + dialog.FileName;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ExportRegistryPack()
    {
        if (_currentHive != RegistryHive.CurrentUser ||
            (!_currentPath.Equals("Software", StringComparison.OrdinalIgnoreCase) &&
             !_currentPath.StartsWith("Software\\\\", StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Shareable .regimp packs can only be created from keys inside HKEY_CURRENT_USER\\\\Software.",
                "Unsupported registry location", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var name = Prompt("Give this registry pack a name:", "Create shareable pack");
        if (name is null) return;
        var description = Prompt("Briefly describe what this pack changes:", "Pack description") ?? "";
        using var dialog = new SaveFileDialog
        {
            Filter = "Mogley Registry Packs (*.regimp)|*.regimp",
            FileName = string.Join("-", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)) + ".regimp",
            DefaultExt = "regimp"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            RegistryPackStore.Export(dialog.FileName, name, description, _currentPath, _currentValues);
            MessageBox.Show(this, "Shareable pack created. It contains only values from the selected key. Imported packs are restricted to HKEY_CURRENT_USER\\Software and are previewed before applying.",
                "Pack exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static string FormatRegValue(RegistryValueItem item)
    {
        var name = item.Name.Length == 0 ? "@" : "\"" + EscapeReg(item.Name) + "\"";
        var value = item.Kind switch
        {
            RegistryValueKind.DWord => "dword:" + unchecked((uint)Convert.ToInt32(item.Data)).ToString("x8"),
            RegistryValueKind.QWord => "hex(b):" + Hex(BitConverter.GetBytes(Convert.ToInt64(item.Data))),
            RegistryValueKind.Binary => "hex:" + Hex((byte[])item.Data!),
            RegistryValueKind.ExpandString => "hex(2):" + Hex(System.Text.Encoding.Unicode.GetBytes(Convert.ToString(item.Data) + "\0")),
            RegistryValueKind.MultiString => "hex(7):" + Hex(System.Text.Encoding.Unicode.GetBytes(string.Join("\0", (string[])item.Data!) + "\0\0")),
            _ => "\"" + EscapeReg(Convert.ToString(item.Data) ?? "") + "\""
        };
        return name + "=" + value;
    }

    private static string Hex(byte[] bytes) => string.Join(",", bytes.Select(x => x.ToString("x2")));
    private static string EscapeReg(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

    private void RestoreBackup()
    {
        Directory.CreateDirectory(_backups.Folder);
        using var dialog = new OpenFileDialog
        {
            Filter = "Mogley registry backups (*.regbu;*.json)|*.regbu;*.json",
            InitialDirectory = _backups.Folder
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!Confirm("Restore this saved registry value? The current value may be overwritten."))
            return;
        try
        {
            _backups.Restore(dialog.FileName);
            LoadSelectedKey();
            MessageBox.Show(this, "Backup restored.", "Restore complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void OpenTweaks()
    {
        using var form = new TweaksForm(_registry, _backups);
        form.ShowDialog(this);
    }

    private void OpenBackupFolder()
    {
        Directory.CreateDirectory(_backups.Folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", _backups.Folder)
        {
            UseShellExecute = true
        });
    }

    private string CurrentFullPath() => RegistryStore.HiveName(_currentHive) +
        (string.IsNullOrEmpty(_currentPath) ? "" : "\\" + _currentPath);

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Confirm registry operation",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private static string? Prompt(string message, string title)
    {
        using var form = new Form
        {
            Text = title, Width = 430, Height = 160, StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false
        };
        var label = new Label { Text = message, Left = 12, Top = 12, Width = 390, AutoSize = true };
        var input = new TextBox { Left = 12, Top = 42, Width = 390 };
        var ok = new Button { Text = "Create", Left = 238, Top = 82, Width = 78, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 324, Top = 82, Width = 78, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { label, input, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
    }

    private void ShowError(Exception ex)
    {
        MessageBox.Show(this, ex.Message, "Registry operation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        _status.Text = ex.Message;
    }

    private void ApplyTheme()
    {
        var p = _colors;
        BackColor = p.Back;
        ForeColor = p.Text;
        ApplyThemeTo(this, p);
        _path.BackColor = p.Surface;
        _path.ForeColor = p.Muted;
    }

    private static void ApplyThemeTo(Control parent, AppColors p)
    {
        foreach (Control control in parent.Controls)
        {
            control.BackColor = control switch
            {
                DataGridView => p.Surface,
                TextBox or ComboBox => p.Input,
                SplitContainer => p.Back,
                _ => p.Back
            };
            control.ForeColor = p.Text;

            if (control is DataGridView grid)
            {
                grid.BackgroundColor = p.Surface;
                grid.GridColor = p.Border;
                grid.DefaultCellStyle.BackColor = p.Surface;
                grid.DefaultCellStyle.ForeColor = p.Text;
                grid.DefaultCellStyle.SelectionBackColor = p.Accent;
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.BackColor = p.Back;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = p.Text;
                grid.EnableHeadersVisualStyles = false;
            }
            else if (control is TreeView tree)
            {
                tree.BackColor = p.Surface;
                tree.ForeColor = p.Text;
                tree.LineColor = p.Muted;
            }
            else if (control is ToolStrip strip)
            {
                strip.BackColor = p.Surface;
                strip.ForeColor = p.Text;
                foreach (ToolStripItem item in strip.Items)
                {
                    item.ForeColor = p.Text;
                    if (item is ToolStripControlHost host)
                    {
                        host.Control.BackColor = p.Input;
                        host.Control.ForeColor = p.Text;
                    }
                }
            }
            else if (control is SplitContainer split)
            {
                split.BackColor = p.Border;
                split.Panel1.BackColor = p.Surface;
                split.Panel2.BackColor = p.Back;
            }
            ApplyThemeTo(control, p);
        }
    }
}
