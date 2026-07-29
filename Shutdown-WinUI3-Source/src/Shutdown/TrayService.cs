using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ShutdownApp;

public sealed class TrayService : IDisposable
{
    private const uint TrayMessage = NativeMethods.WM_APP + 1;
    private const int HotkeyId = 669;
    private const uint IdRestart = 1001;
    private const uint IdShutdown = 1002;
    private const uint IdRdp = 1003;
    private const uint IdSettings = 1004;
    private const uint IdExit = 1005;

    private readonly SettingsStore _settings;
    private readonly NativeMethods.WndProc _wndProc;
    private readonly DispatcherQueue _dispatcher;
    private nint _window;
    private nint _trayIcon;
    private NativeMethods.NOTIFYICONDATA _notifyData;
    private SettingsWindow? _settingsWindow;

    public TrayService(SettingsStore settings)
    {
        _settings = settings;
        _wndProc = WindowProc;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public void Initialize()
    {
        string className = "sol669.Shutdown.TrayWindow";
        nint instance = NativeMethods.GetModuleHandle(null);
        var wc = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = _wndProc,
            hInstance = instance,
            lpszClassName = className
        };
        NativeMethods.RegisterClassEx(ref wc);
        _window = NativeMethods.CreateWindowEx(0, className, "Shutdown Trey", 0, 0, 0, 0, 0,
            nint.Zero, nint.Zero, instance, nint.Zero);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Off.ico");
        _trayIcon = NativeMethods.LoadImage(nint.Zero, iconPath, NativeMethods.IMAGE_ICON, 0, 0,
            NativeMethods.LR_LOADFROMFILE | NativeMethods.LR_DEFAULTSIZE);

        _notifyData = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _window,
            uID = 1,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
            uCallbackMessage = TrayMessage,
            hIcon = _trayIcon,
            szTip = "Shutdown Trey",
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _notifyData);
        RefreshHotkey();
    }

    public void RefreshHotkey()
    {
        if (_window == nint.Zero) return;
        NativeMethods.UnregisterHotKey(_window, HotkeyId);
        if (_settings.Current.EnableRdpHotkey)
            NativeMethods.RegisterHotKey(_window, HotkeyId,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_SHIFT,
                NativeMethods.VK_Q);
    }

    public void RefreshAfterSettingsChanged()
    {
        RefreshHotkey();
    }

    private nint WindowProc(nint hWnd, uint msg, nuint wParam, nint lParam)
    {
        try
        {
            if (msg == TrayMessage)
            {
                uint mouseMessage = unchecked((uint)lParam.ToInt64());
                if (mouseMessage == NativeMethods.WM_RBUTTONUP)
                    ShowMenu();
                else if (mouseMessage == NativeMethods.WM_LBUTTONDBLCLK)
                    _dispatcher.TryEnqueue(() =>
                        _ = PerformPowerActionAsync(_settings.Current.DefaultAction == DefaultPowerAction.Restart));
                return nint.Zero;
            }

            if (msg == NativeMethods.WM_HOTKEY && (int)wParam == HotkeyId)
            {
                _dispatcher.TryEnqueue(SystemActions.DisconnectRdp);
                return nint.Zero;
            }

            if (msg == NativeMethods.WM_DESTROY)
                return nint.Zero;
        }
        catch (Exception ex)
        {
            SettingsStore.Log(ex);
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowMenu()
    {
        NativeTheme.Apply(_settings.Current.Theme, _window);
        nint menu = NativeMethods.CreatePopupMenu();
        try
        {
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, IdRestart, Strings.Restart);
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, IdShutdown, Strings.Shutdown);
            uint defaultCommand = _settings.Current.DefaultAction == DefaultPowerAction.Restart
                ? IdRestart
                : IdShutdown;
            NativeMethods.SetMenuDefaultItem(menu, defaultCommand, 0);
            if (_settings.Current.ShowRdpMenu)
                NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, IdRdp,
                    Strings.DisconnectRdp + "\tCtrl+Alt+Shift+Q");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, IdSettings, Strings.Settings);
            NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, IdExit, Strings.Exit);

            NativeMethods.GetCursorPos(out var point);
            NativeMethods.SetForegroundWindow(_window);
            uint command = NativeMethods.TrackPopupMenu(menu,
                NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_RETURNCMD,
                point.X, point.Y, 0, _window, nint.Zero);
            NativeMethods.PostMessage(_window, 0, 0, 0);

            _dispatcher.TryEnqueue(() => ExecuteCommand(command));
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    private void ExecuteCommand(uint command)
    {
        switch (command)
        {
            case IdRestart:
                _ = PerformPowerActionAsync(true);
                break;
            case IdShutdown:
                _ = PerformPowerActionAsync(false);
                break;
            case IdRdp:
                SystemActions.DisconnectRdp();
                break;
            case IdSettings:
                ShowSettings();
                break;
            case IdExit:
                App.Quit();
                break;
        }
    }

    private async System.Threading.Tasks.Task PerformPowerActionAsync(bool restart)
    {
        var current = _settings.Current;
        bool confirmed = current.ConfirmationMode switch
        {
            ConfirmationMode.None => true,
            ConfirmationMode.Ask => await ConfirmWindow.ShowAsync(restart, null),
            _ => await ConfirmWindow.ShowAsync(restart, current.CountdownSeconds)
        };

        if (!confirmed) return;
        if (restart) SystemActions.Restart();
        else SystemActions.Shutdown();
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Activate();
    }

    public void Dispose()
    {
        if (_window != nint.Zero)
        {
            NativeMethods.UnregisterHotKey(_window, HotkeyId);
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _notifyData);
            NativeMethods.DestroyWindow(_window);
            _window = nint.Zero;
        }
        if (_trayIcon != nint.Zero)
        {
            NativeMethods.DestroyIcon(_trayIcon);
            _trayIcon = nint.Zero;
        }
    }
}
