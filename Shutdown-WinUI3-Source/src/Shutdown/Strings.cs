namespace ShutdownApp;

public static class Strings
{
    public static bool Ru => App.Settings.Current.Language == AppLanguage.Russian;

    public static string Restart => Ru ? "Перезапуск" : "Restart";
    public static string Shutdown => Ru ? "Завершение работы" : "Shut down";
    public static string DisconnectRdp => Ru ? "Выйти из RDP" : "Disconnect RDP";
    public static string Settings => Ru ? "Настройки" : "Settings";
    public static string Exit => Ru ? "Выход" : "Exit";
    public static string ConfirmShutdown => Ru ? "Точно выключить компьютер?" : "Shut down the computer?";
    public static string ConfirmRestart => Ru ? "Точно перезагрузить компьютер?" : "Restart the computer?";
    public static string AutoConfirm(int seconds) => Ru
        ? $"Автоматическое подтверждение через {seconds} сек."
        : $"Automatic confirmation in {seconds} sec.";
    public static string Yes => Ru ? "Да" : "Yes";
    public static string No => Ru ? "Нет" : "No";
}
