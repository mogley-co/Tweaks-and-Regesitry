using Microsoft.Win32;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;

namespace MogleyTweaks.Forms;

public sealed class SystemToolsForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Multiline = true, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(112, 28) };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(18) };
    private readonly ListView _startup = MakeList(("Name", 190), ("Command", 570), ("Source", 170));
    private readonly ListView _apps = MakeList(("Application", 260), ("Version", 110), ("Publisher", 190), ("Install location", 330));
    private readonly ListView _processes = MakeList(("Process", 220), ("PID", 90), ("Memory", 130), ("Window", 420));
    private readonly ListView _drives = MakeList(("Drive", 120), ("Format", 100), ("Total", 130), ("Free", 130), ("Used", 120));
    private readonly ListView _network = MakeList(("Adapter", 220), ("Status", 100), ("Type", 140), ("Addresses", 420));
    private readonly bool _dark = Services.ThemeService.IsDark();

    public SystemToolsForm()
    {
        Text = "Tweaks and Registry — System Tools";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1040, 720);
        MinimumSize = new Size(720, 500);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = _dark ? Color.FromArgb(27, 27, 31) : Color.FromArgb(247, 247, 249);
        ForeColor = _dark ? Color.FromArgb(238, 238, 242) : Color.FromArgb(34, 34, 38);

        AddTab("Overview", BuildOverview());
        AddTab("Startup", BuildListPage(_startup, "Startup entries are read from the standard Run registry keys and Startup folders. This page is an inventory; use Windows Settings to enable or disable entries."));
        AddTab("Installed apps", BuildListPage(_apps, "Installed applications are read from Windows uninstall registry entries. Uninstalling is delegated to Windows Settings."));
        AddTab("Processes", BuildListPage(_processes, "Live process snapshot. This page is read-only; use Task Manager for process actions."));
        AddTab("Storage", BuildListPage(_drives, "Local fixed and removable drive capacity reported by Windows."));
        AddTab("Network", BuildListPage(_network, "Network adapter state and assigned IP addresses. No network settings are changed."));
        AddTab("Windows tools", BuildToolsPage());

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var refresh = MakeButton("Refresh all", (_, _) => RefreshAll());
        var close = MakeButton("Close", (_, _) => Close());
        bottom.Controls.Add(close);
        bottom.Controls.Add(refresh);
        Controls.Add(_tabs);
        Controls.Add(bottom);
        RefreshAll();
    }

    private void AddTab(string title, Control content)
    {
        var page = new TabPage(title) { BackColor = _dark ? Color.FromArgb(31, 31, 35) : Color.FromArgb(250, 250, 252), ForeColor = ForeColor, Padding = new Padding(10) };
        page.Controls.Add(content);
        _tabs.TabPages.Add(page);
    }

    private Control BuildOverview()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(14) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new Label { Text = "Your Windows system", AutoSize = true, Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold), Margin = new Padding(0, 0, 0, 10) };
        panel.Controls.Add(heading, 0, 0);
        _summary.Font = new Font("Segoe UI", 10f);
        _summary.AutoEllipsis = false;
        panel.Controls.Add(_summary, 0, 1);
        return panel;
    }

    private Control BuildListPage(ListView list, string note)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(2) };
        var noteLabel = new Label { Text = note, AutoSize = true, MaximumSize = new Size(790, 0), Margin = new Padding(0, 5, 12, 8), ForeColor = _dark ? Color.Gainsboro : Color.DimGray };
        header.Controls.Add(noteLabel);
        panel.SizeChanged += (_, _) => noteLabel.MaximumSize = new Size(Math.Max(260, panel.ClientSize.Width - 150), 0);
        var target = ReferenceEquals(list, _startup) ? "ms-settings:startupapps"
            : ReferenceEquals(list, _apps) ? "ms-settings:appsfeatures"
            : ReferenceEquals(list, _processes) ? "taskmgr.exe"
            : ReferenceEquals(list, _drives) ? "ms-settings:storagesense"
            : "ms-settings:network-status";
        header.Controls.Add(MakeButton(ReferenceEquals(list, _processes) ? "Open Task Manager" : "Manage in Windows", (_, _) => OpenTool(target)));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(list, 0, 1);
        return panel;
    }

    private Control BuildToolsPage()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), AutoScroll = true, WrapContents = true };
        var tools = new (string Name, string Target, string Description)[]
        {
            ("Task Manager", "taskmgr.exe", "View and manage running applications and processes."),
            ("Services", "services.msc", "Inspect Windows services and their startup configuration."),
            ("Device Manager", "devmgmt.msc", "Inspect hardware and device drivers."),
            ("Disk Management", "diskmgmt.msc", "View partitions and manage disks."),
            ("Event Viewer", "eventvwr.msc", "Inspect Windows application and system logs."),
            ("System Information", "msinfo32.exe", "View detailed hardware and software information."),
            ("Resource Monitor", "resmon.exe", "Inspect CPU, memory, disk and network activity."),
            ("Programs and Features", "appwiz.cpl", "Open the classic installed-programs manager."),
            ("Startup Apps", "ms-settings:startupapps", "Manage apps that start when you sign in."),
            ("Storage Settings", "ms-settings:storagesense", "Review Windows storage usage and cleanup options."),
            ("Network Settings", "ms-settings:network-status", "Open Windows network settings."),
            ("Windows Update", "ms-settings:windowsupdate", "Check update status and options.")
        };
        foreach (var tool in tools)
        {
            var card = new Panel { Width = 300, Height = 118, Margin = new Padding(7), Padding = new Padding(10), BackColor = _dark ? Color.FromArgb(42, 42, 48) : Color.White, BorderStyle = BorderStyle.FixedSingle };
            var title = new Label { Text = tool.Name, AutoSize = true, Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), Location = new Point(10, 8) };
            var description = new Label { Text = tool.Description, AutoSize = true, MaximumSize = new Size(270, 0), ForeColor = _dark ? Color.Silver : Color.DimGray, Location = new Point(10, 34) };
            var open = MakeButton("Open", (_, _) => OpenTool(tool.Target));
            open.Width = 76;
            card.Controls.Add(title);
            card.Controls.Add(description);
            card.Controls.Add(open);
            void LayoutCard()
            {
                card.Width = Math.Max(230, Math.Min(360, (panel.ClientSize.Width - 52) / 2));
                description.MaximumSize = new Size(Math.Max(190, card.ClientSize.Width - 24), 0);
                open.Location = new Point(card.ClientSize.Width - open.Width - 10, card.ClientSize.Height - open.Height - 8);
            }
            card.Resize += (_, _) => LayoutCard();
            panel.Controls.Add(card);
            LayoutCard();
        }
        return panel;
    }

    private void RefreshAll()
    {
        RefreshOverview();
        RefreshStartup();
        RefreshApps();
        RefreshProcesses();
        RefreshDrives();
        RefreshNetwork();
    }

    private void RefreshOverview()
    {
        var osName = ReadRegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName") ?? "Windows";
        var displayVersion = ReadRegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion") ?? "";
        var build = ReadRegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber") ?? Environment.OSVersion.Version.Build.ToString();
        var edition = ReadRegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID") ?? "";
        var machine = Environment.MachineName;
        var user = Environment.UserName;
        var arch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        var runtime = Environment.Version;
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
        var lines = new List<string>
        {
            $"{osName} {displayVersion} (Build {build})",
            $"Edition: {edition}",
            $"Device: {machine}",
            $"Signed-in account: {user}",
            $"Operating system: {arch}",
            $"Application runtime: .NET {runtime}",
            $"System uptime: {uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes",
            "",
            "Available drives:"
        };
        lines.AddRange(drives.Select(d => $"  {d.Name}  —  {FormatBytes(d.AvailableFreeSpace)} free of {FormatBytes(d.TotalSize)}"));
        lines.Add("");
        lines.Add("Use the other tabs for startup entries, installed applications, processes, storage and network adapters.");
        _summary.Text = string.Join(Environment.NewLine, lines);
    }

    private void RefreshStartup()
    {
        _startup.BeginUpdate();
        _startup.Items.Clear();
        var locations = new[]
        {
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "Current user"),
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "All users"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "32-bit all users")
        };
        foreach (var (hive, path, source) in locations)
        {
            try
            {
                using var key = hive.OpenSubKey(path);
                if (key is null) continue;
                foreach (var name in key.GetValueNames())
                    _startup.Items.Add(new ListViewItem(new[] { name, Convert.ToString(key.GetValue(name)) ?? "", source }));
            }
            catch { }
        }
        AddStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Current user folder");
        AddStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "All users folder");
        _startup.EndUpdate();
    }

    private void AddStartupFolder(string folder, string source)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var file in Directory.EnumerateFiles(folder))
                _startup.Items.Add(new ListViewItem(new[] { Path.GetFileName(file), file, source }));
        }
        catch { }
    }

    private void RefreshApps()
    {
        _apps.BeginUpdate();
        _apps.Items.Clear();
        var paths = new[]
        {
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
        };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hive, path) in paths)
        {
            try
            {
                using var root = hive.OpenSubKey(path);
                if (root is null) continue;
                foreach (var subName in root.GetSubKeyNames())
                {
                    try
                    {
                        using var app = root.OpenSubKey(subName);
                        var name = Convert.ToString(app?.GetValue("DisplayName"));
                        if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;
                        var version = Convert.ToString(app?.GetValue("DisplayVersion")) ?? "";
                        var publisher = Convert.ToString(app?.GetValue("Publisher")) ?? "";
                        var location = Convert.ToString(app?.GetValue("InstallLocation")) ?? "";
                        _apps.Items.Add(new ListViewItem(new[] { name, version, publisher, location }));
                    }
                    catch { }
                }
            }
            catch { }
        }
        _apps.EndUpdate();
    }

    private void RefreshProcesses()
    {
        _processes.BeginUpdate();
        _processes.Items.Clear();
        foreach (var process in Process.GetProcesses().OrderBy(p => SafeProcessName(p), StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var memory = process.WorkingSet64;
                var title = process.MainWindowTitle;
                _processes.Items.Add(new ListViewItem(new[] { SafeProcessName(process), process.Id.ToString(), FormatBytes(memory), title }));
            }
            catch { }
            finally { process.Dispose(); }
        }
        _processes.EndUpdate();
    }

    private void RefreshDrives()
    {
        _drives.BeginUpdate();
        _drives.Items.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady) continue;
                var used = drive.TotalSize - drive.AvailableFreeSpace;
                _drives.Items.Add(new ListViewItem(new[] { drive.Name, drive.DriveFormat, FormatBytes(drive.TotalSize), FormatBytes(drive.AvailableFreeSpace), FormatBytes(used) }));
            }
            catch { }
        }
        _drives.EndUpdate();
    }

    private void RefreshNetwork()
    {
        _network.BeginUpdate();
        _network.Items.Clear();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            try
            {
                var addresses = adapter.GetIPProperties().UnicastAddresses.Select(a => a.Address.ToString());
                _network.Items.Add(new ListViewItem(new[] { adapter.Name, adapter.OperationalStatus.ToString(), adapter.NetworkInterfaceType.ToString(), string.Join(", ", addresses) }));
            }
            catch { }
        }
        _network.EndUpdate();
    }

    private static string SafeProcessName(Process process)
    {
        try { return process.ProcessName; } catch { return "(inaccessible)"; }
    }

    private static string? ReadRegistryString(RegistryKey hive, string path, string name)
    {
        try { using var key = hive.OpenSubKey(path); return Convert.ToString(key?.GetValue(name)); }
        catch { return null; }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = Math.Max(0, bytes);
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:0.##} {units[unit]}";
    }

    private static ListView MakeList(params (string Name, int Width)[] columns)
    {
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, HideSelection = false, MinimumSize = new Size(0, 120) };
        foreach (var (name, width) in columns) list.Columns.Add(name, width);
        void ResizeColumns()
        {
            if (list.Columns.Count == 0 || list.ClientSize.Width <= 0) return;
            var weights = columns.Select(x => x.Width).ToArray();
            var available = Math.Max(240, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
            var total = weights.Sum();
            for (var i = 0; i < weights.Length; i++)
                list.Columns[i].Width = Math.Max(55, (int)(available * (double)weights[i] / total));
        }
        list.Resize += (_, _) => ResizeColumns();
        list.HandleCreated += (_, _) => ResizeColumns();
        return list;
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = _dark ? Color.FromArgb(105, 80, 205) : Color.FromArgb(104, 78, 210), ForeColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(5) };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        return button;
    }

    private static void OpenTool(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open this Windows tool.\n\n" + ex.Message, "Open tool failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
