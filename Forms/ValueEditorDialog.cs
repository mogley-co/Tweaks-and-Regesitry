using Microsoft.Win32;

namespace MogleyTweaks.Forms;

public sealed class ValueEditorDialog : Form
{
    private readonly TextBox _name = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _kind = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _data = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };

    public string ValueName => _name.Text.Trim();
    public RegistryValueKind ValueKind => (RegistryValueKind)_kind.SelectedItem!;
    public object ParsedData => Parse(_data.Text, ValueKind);

    public ValueEditorDialog(string name, object? data, RegistryValueKind kind, bool creating)
    {
        Text = creating ? "Create registry value" : "Edit registry value";
        StartPosition = FormStartPosition.CenterParent;
        Width = 540; Height = 390; MinimumSize = new Size(460, 320);
        FormBorderStyle = FormBorderStyle.Sizable;

        _kind.Items.AddRange(new object[]
        {
            RegistryValueKind.String, RegistryValueKind.ExpandString,
            RegistryValueKind.DWord, RegistryValueKind.QWord,
            RegistryValueKind.Binary, RegistryValueKind.MultiString
        });
        _kind.SelectedItem = kind;
        _name.Text = name;
        _data.Text = Format(data);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(14),
            ColumnCount = 1, RowCount = 5
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Value name (blank means Default)", AutoSize = true }, 0, 0);
        layout.Controls.Add(_name, 0, 1);
        layout.Controls.Add(new Label { Text = "Value type", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 2);
        layout.Controls.Add(_kind, 0, 3);
        layout.Controls.Add(_data, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };
        var save = new Button { Text = "Save", Width = 88, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Width = 88, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;

        _kind.SelectedIndexChanged += (_, _) =>
        {
            _data.PlaceholderText = ValueKind switch
            {
                RegistryValueKind.Binary => "Hex bytes separated by spaces or commas: 01 0A FF",
                RegistryValueKind.MultiString => "Enter one string per line",
                RegistryValueKind.DWord => "32-bit integer; hexadecimal may start with 0x",
                RegistryValueKind.QWord => "64-bit integer; hexadecimal may start with 0x",
                _ => "Value data"
            };
        };

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;
            try
            {
                if (string.IsNullOrWhiteSpace(_name.Text) && _name.Text.Length > 0)
                    throw new FormatException("Invalid value name.");
                _ = ParsedData;
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                MessageBox.Show(this, ex.Message, "Invalid value data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        byte[] bytes => string.Join(" ", bytes.Select(x => x.ToString("X2"))),
        string[] lines => string.Join(Environment.NewLine, lines),
        _ => Convert.ToString(value) ?? ""
    };

    private static object Parse(string text, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String or RegistryValueKind.ExpandString => text,
        RegistryValueKind.DWord => unchecked((int)ParseUnsigned(text, uint.MaxValue)),
        RegistryValueKind.QWord => unchecked((long)ParseUnsigned(text, ulong.MaxValue)),
        RegistryValueKind.Binary => text.Split(new[] { ' ', ',', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Convert.ToByte(x, 16)).ToArray(),
        RegistryValueKind.MultiString => text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
        _ => throw new InvalidOperationException("Unsupported registry value type.")
    };

    private static ulong ParseUnsigned(string text, ulong max)
    {
        text = text.Trim();
        var number = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToUInt64(text[2..], 16)
            : Convert.ToUInt64(text, System.Globalization.CultureInfo.InvariantCulture);
        if (number > max) throw new OverflowException("The number is outside the allowed range.");
        return number;
    }
}
