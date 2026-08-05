namespace ShutdownApp;

public enum ConfirmationMode
{
    None,
    Ask,
    Countdown
}

public enum PowerActionKind
{
    Shutdown,
    Restart,
    Sleep,
    Hibernate,
    Lock
}

[System.Flags]
public enum EnabledPowerActions
{
    None = 0,
    Shutdown = 1 << 0,
    Restart = 1 << 1,
    Sleep = 1 << 2,
    Hibernate = 1 << 3,
    Lock = 1 << 4
}

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum AppLanguage
{
    Russian,
    English
}

public sealed class AppSettings
{
    public ConfirmationMode ConfirmationMode { get; set; } = ConfirmationMode.Countdown;
    public PowerActionKind DefaultAction { get; set; } = PowerActionKind.Shutdown;
    public EnabledPowerActions EnabledActions { get; set; } =
        EnabledPowerActions.Shutdown | EnabledPowerActions.Restart;
    public int CountdownSeconds { get; set; } = 5;
    public bool ShowRdpMenu { get; set; } = true;
    public bool ShowScheduledActions { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.System;
    public AppLanguage Language { get; set; } = DetectLanguage();

    private static AppLanguage DetectLanguage() =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ru", System.StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Russian
            : AppLanguage.English;
}
