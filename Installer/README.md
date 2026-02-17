# АСУИПП — Сборка установщика

## Подготовка

### 1. Установите Inno Setup 6
Скачайте с https://jrsoftware.org/isdl.php и установите.
При установке выберите **Russian** в списке языков.

### 2. Структура папок

Поместите папку `Installer` рядом с файлом решения:

```
asuIPP/
├── ASUIPP.App/
├── ASUIPP.Core/
├── Installer/           ← эта папка
│   ├── ASUIPP_Setup.iss
│   ├── build_installer.bat
│   ├── setup_icon.ico
│   ├── app_icon.ico
│   ├── wizard_banner.bmp
│   ├── wizard_small.bmp
│   └── README.md
├── asuIPP.sln
└── packages/
```

## Сборка

### Автоматическая (рекомендуется)
1. Дважды кликните `build_installer.bat`
2. Скрипт автоматически:
   - Найдёт MSBuild
   - Восстановит NuGet пакеты
   - Скомпилирует проект в Release
   - Проверит наличие всех файлов
   - Создаст установщик
3. Результат: `Output/ASUIPP_Setup_1.0.0.exe`

### Ручная
1. В Visual Studio: Build → Configuration Manager → Release → Build Solution
2. Откройте `ASUIPP_Setup.iss` в Inno Setup
3. Build → Compile (Ctrl+F9)
4. Результат: `Output/ASUIPP_Setup_1.0.0.exe`

## Что делает установщик

- Проверяет наличие .NET Framework 4.7.2
- Устанавливает приложение в `Program Files\ASUIPP`
- Создаёт ярлык в меню Пуск
- Опционально: ярлык на рабочем столе
- Опционально: автозапуск при входе в Windows (--tray)
- При удалении: закрывает приложение, удаляет файлы и данные пользователя

## Тестирование на виртуальной машине

1. Скопируйте `ASUIPP_Setup_1.0.0.exe` на ВМ
2. Запустите установщик
3. Если .NET 4.7.2 не установлен — установщик сообщит об этом
4. После установки приложение запустится автоматически
