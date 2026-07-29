using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace ShutdownApp;

public partial class App : Application
{
    private Mutex? _singleInstance;
    internal static TrayService? Tray { get; private set; }
    internal static SettingsStore Settings { get; } = new();

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            SettingsStore.Log(e.Exception);
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstance = new Mutex(true, "sol669.Shutdown.Singleton", out bool createdNew);
        if (!createdNew)
        {
            Exit();
            return;
        }

        Settings.Load();
        Settings.ApplyAutostart();
        Tray = new TrayService(Settings);
        Tray.Initialize();
    }

    internal static void Quit()
    {
        Tray?.Dispose();
        Current.Exit();
    }
}
