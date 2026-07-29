# Shutdown

Минималистичная утилита для Windows 11: выключение и перезагрузка из трея, подтверждение с таймером, выход из RDP, горячая клавиша, автозапуск, темы и русский/английский интерфейс.

A minimalist Windows 11 tray utility for shutdown and restart, timed confirmation, RDP disconnect, hotkey, startup, themes, and Russian/English interface.

## Возможности

- нативное системное меню в трее;
- двойной клик по значку для завершения работы;
- выключение и перезагрузка с подтверждением или обратным отсчётом;
- отключение текущего RDP-сеанса через `tsdiscon`;
- глобальная горячая клавиша `Ctrl+Alt+Shift+Q`;
- автозапуск вместе с Windows;
- светлая, тёмная или системная тема;
- русский и английский интерфейс;
- современное окно настроек на WinUI 3;
- защита от запуска нескольких копий.

## Скачать готовую сборку

После загрузки проекта GitHub автоматически запустит workflow **Build portable**.

1. Открой вкладку **Actions**.
2. Выбери последний успешный запуск **Build portable**.
3. Внизу страницы скачай artifact `Shutdown-Portable-win-x64`.
4. Распакуй архив и запусти `Shutdown.exe`.

## Где хранятся настройки

`%LOCALAPPDATA%\Shutdown\settings.json`

## Сборка локально

Требуются Windows, .NET 8 SDK и Windows App SDK.

```powershell
dotnet publish src/Shutdown/Shutdown.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o publish
```

## Автор

[sol669](https://github.com/sol669)

## Лицензия

MIT
