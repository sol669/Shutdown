using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace ShutdownApp;

internal static class NativeTheme
{
    private enum PreferredAppMode
    {
        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max
    }

    internal static void Apply(AppTheme theme, nint window)
    {
        try
        {
            bool dark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                _ => IsWindowsDark()
            };

            SetPreferredAppMode(dark ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight);
            SetWindowTheme(window, dark ? "DarkMode_Explorer" : "Explorer", null);
            FlushMenuThemes();
        }
        catch
        {
            // Undocumented theme entry points can differ between Windows builds.
            // The menu remains fully functional and falls back to the system default.
        }
    }

    internal static void ApplyWindowTitleBar(AppTheme theme, nint window)
    {
        try
        {
            bool dark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                _ => IsWindowsDark()
            };

            int enabled = dark ? 1 : 0;
            int result = DwmSetWindowAttribute(window, 20, ref enabled, sizeof(int));
            if (result != 0)
                DwmSetWindowAttribute(window, 19, ref enabled, sizeof(int));

            SetWindowTheme(window, dark ? "DarkMode_Explorer" : "Explorer", null);
        }
        catch
        {
            // Fall back to the Windows default title bar on unsupported builds.
        }
    }

    private static bool IsWindowsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(nint hwnd, string? pszSubAppName, string? pszSubIdList);
}
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace ShutdownApp;

internal static class NativeTheme
{
    private enum PreferredAppMode
    {
        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max
    }

    internal static void Apply(AppTheme theme, nint window)
    {
        try
        {
            bool dark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                _ => IsWindowsDark()
            };

            SetPreferredAppMode(dark ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight);
            SetWindowTheme(window, dark ? "DarkMode_Explorer" : "Explorer", null);
            FlushMenuThemes();
        }
        catch
        {
            // Undocumented theme entry points can differ between Windows builds.
            // The menu remains fully functional and falls back to the system default.
        }
    }

    private static bool IsWindowsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(nint hwnd, string? pszSubAppName, string? pszSubIdList);
}
