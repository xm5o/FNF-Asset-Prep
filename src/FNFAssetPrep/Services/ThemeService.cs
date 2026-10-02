using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace FNFAssetPrep.Services;

public static class ThemeService
{
    public static string ResolvedTheme { get; private set; } = "Light";

    public static void Apply(string mode)
    {
        var dark = mode.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            || (mode.Equals("System", StringComparison.OrdinalIgnoreCase) && IsSystemDark());

        ResolvedTheme = dark ? "Dark" : "Light";

        if (dark)
        {
            SetBrush("AppBackgroundBrush", "#151515");
            SetBrush("SidebarBrush", "#1B1B1B");
            SetBrush("SurfaceBrush", "#202020");
            SetBrush("SurfaceAltBrush", "#262626");
            SetBrush("BorderBrush", "#3A3A3A");
            SetBrush("TextBrush", "#F2F2F2");
            SetBrush("MutedTextBrush", "#A7A7A7");
            SetBrush("AccentBrush", "#69A7E8");
            SetBrush("AccentTextBrush", "#0D1A26");
            SetBrush("DangerBrush", "#FF8B82");
        }
        else
        {
            SetBrush("AppBackgroundBrush", "#F3F3F3");
            SetBrush("SidebarBrush", "#F8F8F8");
            SetBrush("SurfaceBrush", "#FFFFFF");
            SetBrush("SurfaceAltBrush", "#FAFAFA");
            SetBrush("BorderBrush", "#D8D8D8");
            SetBrush("TextBrush", "#171717");
            SetBrush("MutedTextBrush", "#666666");
            SetBrush("AccentBrush", "#3178C6");
            SetBrush("AccentTextBrush", "#FFFFFF");
            SetBrush("DangerBrush", "#B42318");
        }
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void SetBrush(string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        Application.Current.Resources[key] = new SolidColorBrush(color);
    }
}
