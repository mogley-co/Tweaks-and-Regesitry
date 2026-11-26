using System.Diagnostics;
using MogleyTweaks.Services;

namespace MogleyTweaks.Forms;

public sealed class ActivationForm : Form
{
    private readonly bool _dark = ThemeService.IsDark();
    private readonly TextBox _output = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Consolas", 9.5f), WordWrap = true
    };
    private readonly Label _status = new() { AutoSize = true };

    public ActivationForm()
    {
        Text = "Windows Activation";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 640);
        MinimumSize = new Size(650, 480);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = _dark ? Color.FromArgb(27, 27, 31) : Color.FromArgb(247, 247, 249);
        ForeColor = _dark ? Color.FromArgb(238, 238, 242) : Color.FromArgb(34, 34, 38);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(20),
            ColumnCount = 1, RowCount = 4, BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Windows Activation", AutoSize = true,
            Font = new Font("Segoe UI Semibold", 21, FontStyle.Bold),
            ForeColor = _dark ? Color.FromArgb(239, 235, 255) : Color.FromArgb(40, 35, 70),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = "Check the activation state reported by Windows, inspect licensing details, and open Microsoft's activation tools.",
            AutoSize = true, MaximumSize = new Size(740, 0),
            ForeColor = _dark ? Color.Silver : Color.DimGray,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 1);

        _output.BackColor = _dark ? Color.FromArgb(35, 35, 40) : Color.White;
        _output.ForeColor = _dark ? Color.Gainsboro : Color.FromArgb(35, 35, 40);
        root.Controls.Add(_output, 0, 2);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2,
            Margin = new Padding(0, 10, 0, 0)
        };
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.Add(MakeButton("Check activation", async (_, _) => await RunDiagnostic("/xpr")));
        buttons.Controls.Add(MakeButton("Detailed license info", async (_, _) => await RunDiagnostic("/dlv")));
        buttons.Controls.Add(MakeButton("Activation settings", (_, _) => OpenTarget("ms-settings:activation")));
        buttons.Controls.Add(MakeButton("Activation settings / troubleshooter", (_, _) => OpenTarget("ms-settings:activation")));
        footer.Controls.Add(buttons, 0, 0);
        _status.ForeColor = _dark ? Color.Silver : Color.DimGray;
        _status.Margin = new Padding(0, 8, 0, 0);
        footer.Controls.Add(_status, 0, 1);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);

        Shown += async (_, _) => await RunDiagnostic("/xpr");
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Height = 34,
            BackColor = Color.FromArgb(105, 78, 210), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 6), Padding = new Padding(8, 2, 8, 2)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        return button;
    }

    private async Task RunDiagnostic(string argument)
    {
        try
        {
            _status.Text = "Reading licensing information from Windows…";
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var script = Path.Combine(windows, "System32", "slmgr.vbs");
            var cscript = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cscript.exe");
            if (!File.Exists(script) || !File.Exists(cscript))
                throw new FileNotFoundException("Windows licensing diagnostic tools were not found.");

            var start = new ProcessStartInfo
            {
                FileName = cscript,
                Arguments = $"//Nologo \"{script}\" {argument}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Could not start the Windows licensing diagnostic.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var result = (await stdout) + Environment.NewLine + (await stderr);
            _output.Text = string.IsNullOrWhiteSpace(result)
                ? "Windows returned no text. Open Activation settings for the current status."
                : result.Trim();
            _status.Text = argument == "/xpr"
                ? "Activation status check completed. The result above comes from Windows."
                : "Detailed licensing query completed. Some fields may be hidden by Windows.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not read activation information.";
            _output.Text = ex.Message + Environment.NewLine + Environment.NewLine
                + "You can still open Settings > System > Activation to view the status reported by Windows.";
        }
    }

    private void OpenTarget(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            _status.Text = "Opened Windows Activation settings.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open Windows settings",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
