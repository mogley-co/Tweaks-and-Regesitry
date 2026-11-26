using System.Text.Json;
using Microsoft.Win32;

namespace MogleyTweaks.Services;

public sealed class RegistryPack
{
    public string Name { get; set; } = "Untitled pack";
    public string Description { get; set; } = "";
    public List<RegistryPackEntry> Entries { get; set; } = new();
}

public sealed class RegistryPackEntry
{
    public string Hive { get; set; } = "HKEY_CURRENT_USER";
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "String";
    public JsonElement Data { get; set; }
}

public static class RegistryPackStore
{
    private const string Format = "Mogley.RegistryImport";
    private sealed class FileModel
    {
        public string FormatName { get; set; } = Format;
        public int Version { get; set; } = 1;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<EntryModel> Entries { get; set; } = new();
    }
    private sealed class EntryModel
    {
        public string Hive { get; set; } = "HKEY_CURRENT_USER";
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "String";
        public JsonElement Data { get; set; }
    }

    public static RegistryPack Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > 1024 * 1024) throw new InvalidDataException("Pack file is missing or larger than 1 MB.");
        var model = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The pack is empty.");
        if (model.FormatName != Format || model.Version != 1) throw new InvalidDataException("Unsupported .regimp format or version.");
        if (model.Entries.Count is < 1 or > 200) throw new InvalidDataException("A pack must contain between 1 and 200 values.");

        var pack = new RegistryPack { Name = Clean(model.Name, 80), Description = Clean(model.Description, 300) };
        foreach (var e in model.Entries)
        {
            if (!string.Equals(e.Hive, "HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Imported packs may only modify HKEY_CURRENT_USER.");
            var key = (e.Key ?? "").Trim('\');
            if (!key.StartsWith("Software\\", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("Software", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Pack entries must be inside HKEY_CURRENT_USER\\Software.");
            if (key.Length > 240 || key.Contains("..", StringComparison.Ordinal) || key.Contains('/') || IsBlocked(key))
                throw new InvalidDataException("The pack contains a restricted registry path.");
            if (e.Name.Length > 160 || e.Name.Contains('\\') || e.Name.Contains('/') || e.Kind is null)
                throw new InvalidDataException("The pack contains an invalid value name or type.");
            if (!Enum.TryParse<RegistryValueKind>(e.Kind, true, out var kind) ||
                kind is not (RegistryValueKind.String or RegistryValueKind.ExpandString or RegistryValueKind.DWord or RegistryValueKind.QWord or RegistryValueKind.Binary or RegistryValueKind.MultiString))
                throw new InvalidDataException("The pack contains an unsupported registry value type.");
            ValidateData(e.Data, kind);
            pack.Entries.Add(new RegistryPackEntry { Hive = "HKEY_CURRENT_USER", Key = key, Name = e.Name, Kind = kind.ToString(), Data = e.Data });
        }
        return pack;
    }

    public static void Export(string path, string name, string description, string keyPath, IEnumerable<RegistryValueItem> values)
    {
        var entries = values.Select(v => new EntryModel
        {
            Hive = "HKEY_CURRENT_USER", Key = keyPath, Name = v.Name, Kind = v.Kind.ToString(),
            Data = ToJson(v.Data, v.Kind)
        }).ToList();
        if (entries.Count == 0) throw new InvalidOperationException("There are no values in the selected key to export.");
        if (entries.Count > 200) throw new InvalidOperationException("A pack can contain at most 200 values.");
        var model = new FileModel { Name = Clean(name, 80), Description = Clean(description, 300), Entries = entries };
        File.WriteAllText(path, JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static object GetValue(RegistryPackEntry entry)
    {
        var kind = Enum.Parse<RegistryValueKind>(entry.Kind, true);
        return kind switch
        {
            RegistryValueKind.String or RegistryValueKind.ExpandString => entry.Data.GetString() ?? "",
            RegistryValueKind.DWord => entry.Data.GetInt32(),
            RegistryValueKind.QWord => entry.Data.GetInt64(),
            RegistryValueKind.Binary => Convert.FromBase64String(entry.Data.GetString() ?? ""),
            RegistryValueKind.MultiString => entry.Data.EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
            _ => throw new InvalidDataException("Unsupported value type.")
        };
    }

    private static JsonElement ToJson(object? data, RegistryValueKind kind)
    {
        object serializable = kind switch
        {
            RegistryValueKind.Binary => Convert.ToBase64String((byte[])data!),
            RegistryValueKind.MultiString => (string[])data!,
            _ => data ?? ""
        };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(serializable));
        return doc.RootElement.Clone();
    }

    private static void ValidateData(JsonElement data, RegistryValueKind kind)
    {
        try
        {
            switch (kind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    if (data.GetString()?.Length > 4096) throw new InvalidDataException("Text value is too long.");
                    break;
                case RegistryValueKind.DWord: _ = data.GetInt32(); break;
                case RegistryValueKind.QWord: _ = data.GetInt64(); break;
                case RegistryValueKind.Binary:
                    if (Convert.FromBase64String(data.GetString() ?? "").Length > 65536) throw new InvalidDataException("Binary value is too large.");
                    break;
                case RegistryValueKind.MultiString:
                    if (data.GetArrayLength() > 256 || data.EnumerateArray().Any(x => (x.GetString() ?? "").Length > 1024))
                        throw new InvalidDataException("Multi-string data is too large.");
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("A registry value has invalid data for its declared type.");
        }
    }

    private static bool IsBlocked(string path)
    {
        var p = "\\" + path.ToLowerInvariant() + "\\";
        string[] blocked = { "\\run\\", "\\runonce\\", "\\policies\\", "\\image file execution options\\", "\\appinit_dlls\\" };
        return blocked.Any(p.Contains);
    }

    private static string Clean(string? value, int max) =>
        new((value ?? "").Where(c => !char.IsControl(c)).Take(max).ToArray());
}
