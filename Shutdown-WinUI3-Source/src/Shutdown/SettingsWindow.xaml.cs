using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ShutdownApp;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;
    private bool _loading = true;

    public SettingsWindow(SettingsStore store)
    {
        _store = store;
        InitializeComponent();
        ConfigureWindow();
        LoadValues();
        ApplyLanguage();
        ApplyThemePreview();
        _loading = false;
    }

    private void ConfigureWindow()
    {
        try { SystemBackdrop = new MicaBackdrop(); } catch { }
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeTheme.ApplyWindowTitleBar(_store.Current.Theme, hwnd);
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "ShutdownTrey.ico"));

        const int logicalWidth = 680;
        const int logicalHeight = 810;
        double scale = Math.Max(1, GetDpiForWindow(hwnd) / 96.0);
        int width = (int)Math.Round(logicalWidth * scale);
        int height = (int)Math.Round(logicalHeight * scale);
        NativeMethods.GetCursorPos(out var cursor);
        DisplayArea area = DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Primary);
        var work = area.WorkArea;
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            work.X + Math.Max(0, (work.Width - width) / 2),
            work.Y + Math.Max(0, (work.Height - height) / 2), width, height));

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
        ShutdownToggle.IsOn = s.EnabledActions.HasFlag(EnabledPowerActions.Shutdown);
        RestartToggle.IsOn = s.EnabledActions.HasFlag(EnabledPowerActions.Restart);
        SleepToggle.IsOn = s.EnabledActions.HasFlag(EnabledPowerActions.Sleep);
        HibernateToggle.IsOn = s.EnabledActions.HasFlag(EnabledPowerActions.Hibernate);
        LockToggle.IsOn = s.EnabledActions.HasFlag(EnabledPowerActions.Lock);
        SleepToggle.IsEnabled = SystemActions.IsAvailable(PowerActionKind.Sleep);
        HibernateToggle.IsEnabled = SystemActions.IsAvailable(PowerActionKind.Hibernate);
        ConfirmationCombo.SelectedIndex = (int)s.ConfirmationMode;
        CountdownNumber.Value = s.CountdownSeconds;
        ScheduledMenuToggle.IsOn = s.ShowScheduledActions;
        RdpToggle.IsOn = s.ShowRdpMenu;
        AutostartToggle.IsOn = s.StartWithWindows;
        ThemeCombo.SelectedIndex = (int)s.Theme;
        LanguageCombo.SelectedIndex = s.Language == AppLanguage.Russian ? 0 : 1;
        RebuildDefaultActions(s.DefaultAction);
        UpdateCountdownState();
    }

    private void ApplyLanguage()
    {
        bool ru = _store.Current.Language == AppLanguage.Russian;
        TitleText.Text = ru ? "Настройки" : "Settings";
        ActionsSection.Text = ru ? "ДЕЙСТВИЯ" : "ACTIONS";
        ShutdownLabel.Text = Strings.ActionName(PowerActionKind.Shutdown);
        RestartLabel.Text = Strings.ActionName(PowerActionKind.Restart);
        SleepLabel.Text = Strings.ActionName(PowerActionKind.Sleep) + (!SleepToggle.IsEnabled ? (ru ? " — недоступно" : " — unavailable") : "");
        HibernateLabel.Text = Strings.ActionName(PowerActionKind.Hibernate) + (!HibernateToggle.IsEnabled ? (ru ? " — недоступно" : " — unavailable") : "");
        LockLabel.Text = Strings.ActionName(PowerActionKind.Lock);
        DefaultActionLabel.Text = ru ? "Действие по умолчанию" : "Default action";
        ConfirmationSection.Text = ru ? "ПОДТВЕРЖДЕНИЕ" : "CONFIRMATION";
        ConfirmationLabel.Text = ru ? "Режим" : "Mode";
        NoConfirmationItem.Content = ru ? "Без подтверждения" : "No confirmation";
        AskItem.Content = ru ? "Спрашивать Да / Нет" : "Ask Yes / No";
        CountdownItem.Content = ru ? "С обратным отсчётом" : "With countdown";
        CountdownLabel.Text = ru ? "Обратный отсчёт, сек." : "Countdown, sec.";
        ScheduledSection.Text = ru ? "ОТЛОЖЕННЫЕ ДЕЙСТВИЯ" : "SCHEDULED ACTIONS";
        ScheduledMenuLabel.Text = ru ? "Показывать в меню" : "Show in menu";
        IntegrationSection.Text = ru ? "ИНТЕГРАЦИЯ" : "INTEGRATION";
        RdpLabel.Text = ru ? "Выход из RDP" : "RDP disconnect";
        AutostartLabel.Text = ru ? "Автозапуск" : "Start with Windows";
        InterfaceSection.Text = ru ? "ИНТЕРФЕЙС" : "INTERFACE";
        ThemeLabel.Text = ru ? "Тема" : "Theme";
        SystemThemeItem.Content = ru ? "Как в Windows" : "Use Windows setting";
        LightThemeItem.Content = ru ? "Светлая" : "Light";
        DarkThemeItem.Content = ru ? "Тёмная" : "Dark";
        LanguageLabel.Text = ru ? "Язык" : "Language";
        CancelButton.Content = Strings.Cancel;
        SaveButton.Content = ru ? "Сохранить" : "Save";
    }

    private EnabledPowerActions SelectedActions()
    {
        EnabledPowerActions result = EnabledPowerActions.None;
        if (ShutdownToggle.IsOn) result |= EnabledPowerActions.Shutdown;
        if (RestartToggle.IsOn) result |= EnabledPowerActions.Restart;
        if (SleepToggle.IsOn && SleepToggle.IsEnabled) result |= EnabledPowerActions.Sleep;
        if (HibernateToggle.IsOn && HibernateToggle.IsEnabled) result |= EnabledPowerActions.Hibernate;
        if (LockToggle.IsOn) result |= EnabledPowerActions.Lock;
        return result;
    }

    private void RebuildDefaultActions(PowerActionKind preferred)
    {
        var enabled = SelectedActions();
        if (enabled == EnabledPowerActions.None) enabled = EnabledPowerActions.Shutdown;
        DefaultActionCombo.Items.Clear();
        foreach (PowerActionKind action in Enum.GetValues<PowerActionKind>())
            if (enabled.HasFlag(action.ToFlag()))
                DefaultActionCombo.Items.Add(new ComboBoxItem { Content = Strings.ActionName(action), Tag = action });
        var match = DefaultActionCombo.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (PowerActionKind)x.Tag == preferred);
        DefaultActionCombo.SelectedItem = match ?? DefaultActionCombo.Items[0];
    }

    private void ActionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        PowerActionKind preferred = DefaultActionCombo.SelectedItem is ComboBoxItem item && item.Tag is PowerActionKind action
            ? action : _store.Current.DefaultAction;
        RebuildDefaultActions(preferred);
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
        var enabled = SelectedActions();
        if (enabled == EnabledPowerActions.None)
        {
            enabled = EnabledPowerActions.Shutdown;
            ShutdownToggle.IsOn = true;
        }
        PowerActionKind defaultAction = DefaultActionCombo.SelectedItem is ComboBoxItem selected && selected.Tag is PowerActionKind action
            ? action : enabled.FirstAction();
        _store.Replace(new AppSettings
        {
            ConfirmationMode = (ConfirmationMode)Math.Clamp(ConfirmationCombo.SelectedIndex, 0, 2),
            DefaultAction = defaultAction,
            EnabledActions = enabled,
            CountdownSeconds = Math.Clamp((int)(double.IsNaN(CountdownNumber.Value) ? 5 : CountdownNumber.Value), 1, 300),
            ShowScheduledActions = ScheduledMenuToggle.IsOn,
            ShowRdpMenu = RdpToggle.IsOn,
            StartWithWindows = AutostartToggle.IsOn,
            Theme = (AppTheme)Math.Clamp(ThemeCombo.SelectedIndex, 0, 2),
            Language = LanguageCombo.SelectedIndex == 0 ? AppLanguage.Russian : AppLanguage.English
        });
        App.Tray?.RefreshAfterSettingsChanged();
        Close();
    }

    private void ApplyThemePreview() => RootGrid.RequestedTheme = _store.Current.Theme switch
    {
        AppTheme.Light => ElementTheme.Light,
        AppTheme.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default
    };

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
}
