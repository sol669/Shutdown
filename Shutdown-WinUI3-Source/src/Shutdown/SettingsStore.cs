using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ShutdownApp;

public sealed class SettingsStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Shutdown");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Shutdown";

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            if (!File.Exists(FilePath))
            {
                Current = new AppSettings();
                Save();
                return;
            }

            Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            Current.CountdownSeconds = Math.Clamp(Current.CountdownSeconds, 1, 300);
        }
        catch (Exception ex)
        {
            Log(ex);
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
        ApplyAutostart();
    }

    public void Replace(AppSettings value)
    {
        Current = value;
        Save();
    }

    public void ApplyAutostart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (Current.StartWithWindows)
                key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }

    public static void Log(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.AppendAllText(Path.Combine(Folder, "error.log"),
                $"[{DateTime.Now:O}] {ex}\r\n\r\n");
        }
        catch
        {
            Debug.WriteLine(ex);
        }
    }
}
