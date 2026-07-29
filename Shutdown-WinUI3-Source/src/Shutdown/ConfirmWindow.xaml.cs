using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShutdownApp;

public sealed partial class ConfirmWindow : Window
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer? _timer;
    private readonly int? _initialSeconds;
    private int _remaining;
    private bool _completed;

    private ConfirmWindow(bool restart, int? seconds)
    {
        InitializeComponent();
        _initialSeconds = seconds;
        _remaining = seconds ?? 0;
        QuestionText.Text = restart ? Strings.ConfirmRestart : Strings.ConfirmShutdown;
        YesButton.Content = Strings.Yes;
        NoButton.Content = Strings.No;

        RootGrid.RequestedTheme = App.Settings.Current.Theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        try { SystemBackdrop = new MicaBackdrop(); } catch { }
        ConfigureWindow();
        Closed += ConfirmWindow_Closed;

        if (seconds is null)
        {
            CountdownText.Visibility = Visibility.Collapsed;
            CountdownBar.Visibility = Visibility.Collapsed;
        }
        else
        {
            UpdateCountdown();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
    }

    private void ConfigureWindow()
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeTheme.ApplyWindowTitleBar(App.Settings.Current.Theme, hwnd);
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Off.ico"));
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        const int logicalWidth = 540;
        const int logicalHeight = 280;
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi > 0 ? dpi / 96.0 : 1.0;
        int width = (int)Math.Round(logicalWidth * scale);
        int height = (int)Math.Round(logicalHeight * scale);

        DisplayArea displayArea = DisplayArea.GetFromWindowId(id, DisplayAreaFallback.Primary);
        Windows.Graphics.RectInt32 workArea = displayArea.WorkArea;
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            workArea.X + Math.Max(0, (workArea.Width - width) / 2),
            workArea.Y + Math.Max(0, (workArea.Height - height) / 2),
            width,
            height));
    }

    public static async Task<bool> ShowAsync(bool restart, int? seconds)
    {
        var window = new ConfirmWindow(restart, seconds);
        window.Activate();
        return await window._result.Task;
    }

    private void Timer_Tick(object? sender, object e)
    {
        _remaining--;
        if (_remaining <= 0)
        {
            Complete(true);
            return;
        }
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        CountdownText.Text = Strings.AutoConfirm(_remaining);
        CountdownBar.Value = _initialSeconds is > 0 ? 100.0 * _remaining / _initialSeconds.Value : 0;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e) => Complete(true);
    private void NoButton_Click(object sender, RoutedEventArgs e) => Complete(false);

    private void ConfirmWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_completed)
        {
            _completed = true;
            _timer?.Stop();
            _result.TrySetResult(false);
        }
    }

    private void Complete(bool value)
    {
        if (_completed) return;
        _completed = true;
        _timer?.Stop();
        _result.TrySetResult(value);
        Close();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
