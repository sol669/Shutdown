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

        const int logicalWidth = 760;
        const int logicalHeight = 900;
        double scale = Math.Max(1, GetDpiForWindow(hwnd) / 96.0);
        NativeMethods.GetCursorPos(out var cursor);
        DisplayArea area = DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(cursor.X, cursor.Y), DisplayAreaFallback.Primary);
        var work = area.WorkArea;
        int width = Math.Min((int)Math.Round(logicalWidth * scale), Math.Max(640, work.Width - 48));
        int height = Math.Min((int)Math.Round(logicalHeight * scale), Math.Max(650, work.Height - 48));
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            work.X + Math.Max(0, (work.Width - width) / 2),
            work.Y + Math.Max(0, (work.Height - height) / 2), width, height));

        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
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
        RdpToggle.IsOn = s.UseRdpAsDefaultAction;
        AutostartToggle.IsOn = s.StartWithWindows;
        ThemeCombo.SelectedIndex = (int)s.Theme;
        LanguageCombo.SelectedIndex = s.Language == AppLanguage.Russian ? 0 : 1;
        RebuildDefaultActions(s.DefaultAction);
        UpdateCountdownState();
        UpdateToggleStates();
    }

    private void ApplyLanguage()
    {
        bool ru = _store.Current.Language == AppLanguage.Russian;
        TitleText.Text = ru ? "Настройки" : "Settings";
        ActionsSection.Text = ru ? "ДЕЙСТВИЯ В ТРЕЕ" : "TRAY ACTIONS";
        ShutdownLabel.Text = Strings.ActionName(PowerActionKind.Shutdown);
        RestartLabel.Text = Strings.ActionName(PowerActionKind.Restart);
        SleepLabel.Text = Strings.ActionName(PowerActionKind.Sleep);
        HibernateLabel.Text = Strings.ActionName(PowerActionKind.Hibernate);
        LockLabel.Text = Strings.ActionName(PowerActionKind.Lock);
        DefaultActionLabel.Text = ru ? "Действие по умолчанию" : "Default action";
        BehaviorSection.Text = ru ? "ПОВЕДЕНИЕ" : "BEHAVIOR";
        ConfirmationLabel.Text = ru ? "Подтверждение" : "Confirmation";
        NoConfirmationItem.Content = ru ? "Без подтверждения" : "No confirmation";
        AskItem.Content = ru ? "Спрашивать Да / Нет" : "Ask Yes / No";
        CountdownItem.Content = ru ? "С обратным отсчётом" : "With countdown";
        CountdownLabel.Text = ru ? "Обратный отсчёт, сек." : "Countdown, sec.";
        ScheduledMenuLabel.Text = ru ? "Отложенные действия в меню" : "Scheduled actions in menu";
        SystemSection.Text = ru ? "СИСТЕМА" : "SYSTEM";
        RdpLabel.Text = ru ? "Отключиться — действие по умолчанию при RDP-подключении" : "Disconnect — default action during an RDP session";
        AutostartLabel.Text = ru ? "Автозапуск" : "Autostart";
        ThemeLabel.Text = ru ? "Тема" : "Theme";
        SystemThemeItem.Content = ru ? "Как в Windows" : "Use Windows setting";
        LightThemeItem.Content = ru ? "Светлая" : "Light";
        DarkThemeItem.Content = ru ? "Тёмная" : "Dark";
        LanguageLabel.Text = ru ? "Язык" : "Language";
        CancelButton.Content = Strings.Cancel;
        SaveButton.Content = ru ? "Сохранить" : "Save";
        UpdateToggleStates();
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
        UpdateToggleStates();
        if (_loading) return;
        PowerActionKind preferred = DefaultActionCombo.SelectedItem is ComboBoxItem item && item.Tag is PowerActionKind action
            ? action : _store.Current.DefaultAction;
        RebuildDefaultActions(preferred);
    }

    private void OptionToggle_Toggled(object sender, RoutedEventArgs e) => UpdateToggleStates();

    private void UpdateToggleStates()
    {
        if (ShutdownState is null) return;
        SetToggleState(ShutdownState, ShutdownToggle);
        SetToggleState(RestartState, RestartToggle);
        SetToggleState(SleepState, SleepToggle);
        SetToggleState(HibernateState, HibernateToggle);
        SetToggleState(LockState, LockToggle);
        SetToggleState(ScheduledMenuState, ScheduledMenuToggle);
        SetToggleState(RdpState, RdpToggle);
        SetToggleState(AutostartState, AutostartToggle);
    }

    private void SetToggleState(TextBlock state, ToggleSwitch toggle)
    {
        bool ru = _store.Current.Language == AppLanguage.Russian;
        state.Text = !toggle.IsEnabled
            ? (ru ? "Недоступно" : "Unavailable")
            : toggle.IsOn
                ? (ru ? "Вкл." : "On")
                : (ru ? "Откл." : "Off");
        state.Opacity = toggle.IsEnabled ? 0.72 : 0.45;
    }

    private void ConfirmationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCountdownState();
    private void UpdateCountdownState()
    {
        bool active = ConfirmationCombo.SelectedIndex == (int)ConfirmationMode.Countdown;
        CountdownCard.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
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
            UseRdpAsDefaultAction = RdpToggle.IsOn,
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
