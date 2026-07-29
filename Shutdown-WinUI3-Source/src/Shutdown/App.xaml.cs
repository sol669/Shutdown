using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace ShutdownApp;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private Window? _lifetimeWindow;
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
        CreateLifetimeWindow();
        Tray = new TrayService(Settings);
        Tray.Initialize();
    }

    private void CreateLifetimeWindow()
    {
        _lifetimeWindow = new Window();
        _lifetimeWindow.AppWindow.IsShownInSwitchers = false;
        _lifetimeWindow.AppWindow.Move(new Windows.Graphics.PointInt32(-32000, -32000));
        _lifetimeWindow.Activate();
        _lifetimeWindow.AppWindow.Hide();
    }

    internal static void Quit()
    {
        Tray?.Dispose();
        Current.Exit();
    }
}
