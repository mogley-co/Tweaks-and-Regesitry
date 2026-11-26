using Microsoft.Win32;

namespace MogleyTweaks.Services;

public sealed record RegistryValueItem(string Name, object? Data, RegistryValueKind Kind)
{
    public string DisplayName => string.IsNullOrEmpty(Name) ? "(Default)" : Name;
    public string DisplayData => Data switch
    {
        null => "(null)",
        byte[] bytes => Convert.ToHexString(bytes),
        string[] lines => string.Join(" | ", lines),
        _ => Convert.ToString(Data) ?? ""
    };
}

public sealed class RegistryStore
{
    public static readonly (string Name, RegistryHive Hive)[] Roots =
    {
        ("HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot),
        ("HKEY_CURRENT_USER", RegistryHive.CurrentUser),
        ("HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine),
        ("HKEY_USERS", RegistryHive.Users),
        ("HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig)
    };

    public static string HiveName(RegistryHive hive) =>
        Roots.First(x => x.Hive == hive).Name;

    public static RegistryHive ParseHive(string name) =>
        Roots.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Hive;

    public RegistryKey OpenRead(RegistryHive hive, string path)
    {
        var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        if (string.IsNullOrEmpty(path)) return root;
        var child = root.OpenSubKey(path);
        root.Dispose();
        return child ?? throw new IOException("The selected registry key no longer exists.");
    }

    private RegistryKey OpenWrite(RegistryHive hive, string path)
    {
        var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        if (string.IsNullOrEmpty(path)) return root;
        var child = root.CreateSubKey(path, writable: true);
        root.Dispose();
        return child ?? throw new UnauthorizedAccessException("Windows refused write access to this registry key.");
    }

    public string[] GetSubKeys(RegistryHive hive, string path)
    {
        using var key = OpenRead(hive, path);
        return key.GetSubKeyNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public RegistryValueItem[] GetValues(RegistryHive hive, string path)
    {
        using var key = OpenRead(hive, path);
        var result = new List<RegistryValueItem>();
        foreach (var name in key.GetValueNames())
        {
            try
            {
                var kind = key.GetValueKind(name);
                var data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                result.Add(new RegistryValueItem(name, data, kind));
            }
            catch { /* Skip values Windows cannot decode. */ }
        }
        return result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void SetValue(RegistryHive hive, string path, string name, object data, RegistryValueKind kind)
    {
        using var key = OpenWrite(hive, path);
        key.SetValue(name, data, kind);
    }

    public void DeleteValue(RegistryHive hive, string path, string name)
    {
        using var key = OpenWrite(hive, path);
        key.DeleteValue(name, throwOnMissingValue: true);
    }

    public void CreateKey(RegistryHive hive, string parentPath, string name)
    {
        ValidateSingleName(name);
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var parent = string.IsNullOrEmpty(parentPath) ? root : root.OpenSubKey(parentPath, writable: true);
        if (parent is null) throw new UnauthorizedAccessException("Cannot open the parent key for writing.");
        using var created = parent.CreateSubKey(name, writable: true);
        if (created is null) throw new UnauthorizedAccessException("Windows refused to create the key.");
    }

    public void DeleteEmptyKey(RegistryHive hive, string parentPath, string name)
    {
        ValidateSingleName(name);
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var parent = string.IsNullOrEmpty(parentPath) ? root : root.OpenSubKey(parentPath, writable: true);
        if (parent is null) throw new UnauthorizedAccessException("Cannot open the parent key for writing.");
        parent.DeleteSubKey(name, throwOnMissingSubKey: true);
    }

    private static void ValidateSingleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\\') || name.Contains('/'))
            throw new ArgumentException("Enter one non-empty key name, not a path.");
    }

    public static string Join(string parent, string child) =>
        string.IsNullOrEmpty(parent) ? child : parent + "\\" + child;
}
