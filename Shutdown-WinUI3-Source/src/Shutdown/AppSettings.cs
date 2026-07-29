namespace ShutdownApp;

public enum ConfirmationMode
{
    None,
    Ask,
    Countdown
}

public enum DefaultPowerAction
{
    Shutdown,
    Restart
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
    public DefaultPowerAction DefaultAction { get; set; } = DefaultPowerAction.Shutdown;
    public int CountdownSeconds { get; set; } = 5;
    public bool ShowRdpMenu { get; set; } = true;
    public bool EnableRdpHotkey { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.System;
    public AppLanguage Language { get; set; } = DetectLanguage();

    private static AppLanguage DetectLanguage() =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ru", System.StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Russian
            : AppLanguage.English;
}
