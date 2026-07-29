using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ShutdownApp;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;

    public SettingsWindow(SettingsStore store)
    {
        _store = store;
        InitializeComponent();
        ConfigureWindow();
        LoadValues();
        ApplyLanguage();
        ApplyThemePreview();
    }

    private void ConfigureWindow()
    {
        try { SystemBackdrop = new MicaBackdrop(); } catch { }

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Off.ico"));

        const int logicalWidth = 680;
        const int logicalHeight = 760;
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi > 0 ? dpi / 96.0 : 1.0;
        appWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)Math.Round(logicalWidth * scale),
            (int)Math.Round(logicalHeight * scale)));

        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }
    }

    private void LoadValues()
    {
        var s = _store.Current;
        ConfirmationCombo.SelectedIndex = (int)s.ConfirmationMode;
        CountdownNumber.Value = s.CountdownSeconds;
        RdpMenuToggle.IsOn = s.ShowRdpMenu;
        HotkeyToggle.IsOn = s.EnableRdpHotkey;
        AutostartToggle.IsOn = s.StartWithWindows;
        ThemeCombo.SelectedIndex = (int)s.Theme;
        LanguageCombo.SelectedIndex = s.Language == AppLanguage.Russian ? 0 : 1;
        UpdateCountdownState();
    }

    private void ApplyLanguage()
    {
        bool ru = _store.Current.Language == AppLanguage.Russian;
        TitleText.Text = ru ? "Настройки" : "Settings";
        SubtitleText.Text = ru ? "Поведение приложения, оформление и системные функции" : "App behavior, appearance, and system features";
        ConfirmationLabel.Text = ru ? "Подтверждение действий" : "Action confirmation";
        NoConfirmationItem.Content = ru ? "Без подтверждения" : "No confirmation";
        AskItem.Content = ru ? "Спрашивать Да / Нет" : "Ask Yes / No";
        CountdownItem.Content = ru ? "Подтверждение с таймером" : "Confirmation with countdown";
        CountdownLabel.Text = ru ? "Обратный отсчёт" : "Countdown";
        CountdownHint.Text = ru ? "От 1 до 300 секунд" : "From 1 to 300 seconds";
        RdpMenuLabel.Text = ru ? "Показывать «Выйти из RDP»" : "Show “Disconnect RDP”";
        RdpMenuHint.Text = ru ? "Добавляет команду в меню трея" : "Adds the command to the tray menu";
        HotkeyLabel.Text = ru ? "Горячая клавиша выхода из RDP" : "RDP disconnect hotkey";
        AutostartLabel.Text = ru ? "Запускать вместе с Windows" : "Start with Windows";
        AutostartHint.Text = ru ? "Запускает Shutdown после входа в систему" : "Starts Shutdown after sign-in";
        ThemeLabel.Text = ru ? "Тема" : "Theme";
        SystemThemeItem.Content = ru ? "Как в Windows" : "Use Windows setting";
        LightThemeItem.Content = ru ? "Светлая" : "Light";
        DarkThemeItem.Content = ru ? "Тёмная" : "Dark";
        LanguageLabel.Text = ru ? "Язык" : "Language";
        CancelButton.Content = ru ? "Отмена" : "Cancel";
        SaveButton.Content = ru ? "Сохранить" : "Save";
    }

    private void ApplyThemePreview()
    {
        RootGrid.RequestedTheme = _store.Current.Theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private void ConfirmationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCountdownState();

    private void UpdateCountdownState()
    {
        bool active = ConfirmationCombo.SelectedIndex == (int)ConfirmationMode.Countdown;
        CountdownRow.Opacity = active ? 1 : 0.45;
        CountdownNumber.IsEnabled = active;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var value = new AppSettings
        {
            ConfirmationMode = (ConfirmationMode)Math.Clamp(ConfirmationCombo.SelectedIndex, 0, 2),
            CountdownSeconds = Math.Clamp((int)(double.IsNaN(CountdownNumber.Value) ? 5 : CountdownNumber.Value), 1, 300),
            ShowRdpMenu = RdpMenuToggle.IsOn,
            EnableRdpHotkey = HotkeyToggle.IsOn,
            StartWithWindows = AutostartToggle.IsOn,
            Theme = (AppTheme)Math.Clamp(ThemeCombo.SelectedIndex, 0, 2),
            Language = LanguageCombo.SelectedIndex == 0 ? AppLanguage.Russian : AppLanguage.English
        };
        _store.Replace(value);
        App.Tray?.RefreshAfterSettingsChanged();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
