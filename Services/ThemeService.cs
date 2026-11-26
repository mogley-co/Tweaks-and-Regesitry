using Microsoft.Win32;

namespace MogleyTweaks.Services;

public static class ThemeService
{
    public static bool IsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1) ?? 1) == 0;
        }
        catch
        {
            return false;
        }
    }
}
