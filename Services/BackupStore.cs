using System.Text.Json;
using Microsoft.Win32;

namespace MogleyTweaks.Services;

public sealed class BackupStore
{
    private sealed class ValueBackup
    {
        public string Hive { get; set; } = "";
        public string KeyPath { get; set; } = "";
        public string ValueName { get; set; } = "";
        public bool Existed { get; set; }
        public string Kind { get; set; } = nameof(RegistryValueKind.String);
        public string Data { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MogleyTweaks", "Backups");

    public string SaveValue(RegistryHive hive, string keyPath, string valueName)
    {
        Directory.CreateDirectory(Folder);
        var backup = new ValueBackup
        {
            Hive = RegistryStore.HiveName(hive),
            KeyPath = keyPath,
            ValueName = valueName
        };

        try
        {
            using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Default).OpenSubKey(keyPath);
            if (key is not null && key.GetValueNames().Contains(valueName, StringComparer.OrdinalIgnoreCase))
            {
                var kind = key.GetValueKind(valueName);
                var data = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                backup.Existed = true;
                backup.Kind = kind.ToString();
                backup.Data = Encode(data, kind);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        var file = Path.Combine(Folder, $"value-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
        return file;
    }

    public void Restore(string file)
    {
        var backup = JsonSerializer.Deserialize<ValueBackup>(File.ReadAllText(file))
            ?? throw new InvalidDataException("The backup file is invalid.");
        var hive = RegistryStore.ParseHive(backup.Hive);

        if (!backup.Existed)
        {
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var key = string.IsNullOrEmpty(backup.KeyPath) ? root : root.OpenSubKey(backup.KeyPath, writable: true);
            key?.DeleteValue(backup.ValueName, throwOnMissingValue: false);
            return;
        }

        if (!Enum.TryParse<RegistryValueKind>(backup.Kind, out var kind))
            throw new InvalidDataException("The backup contains an unknown registry value type.");

        var data = Decode(backup.Data, kind);
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var target = string.IsNullOrEmpty(backup.KeyPath)
            ? baseKey
            : baseKey.CreateSubKey(backup.KeyPath, writable: true);
        if (target is null) throw new UnauthorizedAccessException("Cannot open the registry key for restore.");
        target.SetValue(backup.ValueName, data, kind);
    }

    private static string Encode(object? value, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.Binary => Convert.ToBase64String((byte[])value!),
        RegistryValueKind.MultiString => JsonSerializer.Serialize((string[])value!),
        RegistryValueKind.DWord => Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
        RegistryValueKind.QWord => Convert.ToInt64(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToString(value) ?? ""
    };

    private static object Decode(string value, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.Binary => Convert.FromBase64String(value),
        RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(value) ?? Array.Empty<string>(),
        RegistryValueKind.DWord => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        RegistryValueKind.QWord => long.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        RegistryValueKind.String or RegistryValueKind.ExpandString => value,
        _ => throw new InvalidDataException("This registry value type cannot be restored by this edition.")
    };
}
