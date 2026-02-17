using System;
using System.Linq;
using System.Windows;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;
using ASUIPP.App.Views;
using Hardcodet.Wpf.TaskbarNotification;

namespace ASUIPP.App
{
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon;
        private DatabaseContext _dbContext;
        private bool _startMinimized;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Принудительно добавляем путь к SQLite.Interop.dll
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var arch = Environment.Is64BitProcess ? "x64" : "x86";
            var interopPath = System.IO.Path.Combine(appDir, arch);

            // Добавляем в PATH чтобы Windows нашла dll
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!path.Contains(interopPath))
            {
                Environment.SetEnvironmentVariable("PATH", interopPath + ";" + path);
            }

            var styles = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Resources/Styles.xaml")
            };
            Resources.MergedDictionaries.Add(styles);

            _startMinimized = e.Args.Contains("--tray");

            _dbContext = new DatabaseContext();
            var initializer = new DatabaseInitializer(_dbContext);
            initializer.Initialize();

            // Загружаем масштаб ДО создания окон
            LoadGlobalZoom();

            InitTrayIcon();

            var settingsRepo = new SettingsRepository(_dbContext);
            var appSettings = settingsRepo.GetAppSettings();

            if (appSettings.IsFirstRun)
            {
                ShowSetupWindow();
            }
            else if (_startMinimized)
            {
                // Сидим в трее
            }
            else
            {
                ShowMainWindow();
            }
        }

        private void LoadGlobalZoom()
        {
            try
            {
                var settingsRepo = new SettingsRepository(_dbContext);
                var saved = settingsRepo.Get("ZoomLevel");
                if (!string.IsNullOrEmpty(saved) &&
                    double.TryParse(saved, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var level))
                {
                    Helpers.ZoomHelper.Zoom = level;
                }
            }
            catch { }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "АСУИПП",
                Icon = new System.Drawing.Icon(
                    GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/tray_icon.ico")).Stream),
                MenuActivation = PopupActivationMode.RightClick
            };

            var menu = new System.Windows.Controls.ContextMenu();

            var openItem = new System.Windows.Controls.MenuItem { Header = "Открыть АСУИПП" };
            openItem.Click += (s, ev) => ShowMainWindow();
            menu.Items.Add(openItem);

            var reminderItem = new System.Windows.Controls.MenuItem { Header = "Напоминания" };
            reminderItem.Click += (s, ev) => ShowReminderPanel();
            menu.Items.Add(reminderItem);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
            exitItem.Click += (s, ev) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;

            // Двойной клик — основное окно
            _trayIcon.TrayMouseDoubleClick += (s, ev) => ShowMainWindow();
            // Одинарный левый клик — тоже основное окно (не напоминалка)
            _trayIcon.TrayLeftMouseUp += (s, ev) => ShowMainWindow();
        }

        public void ShowSetupWindow()
        {
            var window = new SetupWindow(_dbContext);
            window.SetupCompleted += () =>
            {
                window.Close();
                ShowMainWindow();
            };
            window.Show();
        }

        public void ShowMainWindow()
        {
            // Обязательно в UI-потоке
            Dispatcher.Invoke(() =>
            {
                var existing = Windows.OfType<MainWindow>().FirstOrDefault();
                if (existing != null)
                {
                    existing.Show();
                    existing.WindowState = WindowState.Normal;
                    existing.Activate();
                    existing.Topmost = true;
                    existing.Topmost = false;
                    existing.Focus();
                    return;
                }

                var settingsRepo = new SettingsRepository(_dbContext);
                var appSettings = settingsRepo.GetAppSettings();

                var window = new MainWindow(_dbContext, appSettings);
                window.Show();
                window.Activate();
            });
        }

        public void ShowReminderPanel()
        {
            Dispatcher.Invoke(() =>
            {
                var existing = Windows.OfType<ReminderWindow>().FirstOrDefault();
                if (existing != null)
                {
                    existing.Activate();
                    return;
                }

                var settingsRepo = new SettingsRepository(_dbContext);
                var appSettings = settingsRepo.GetAppSettings();

                if (string.IsNullOrEmpty(appSettings.CurrentTeacherId))
                    return;

                var window = new ReminderWindow(_dbContext, appSettings.CurrentTeacherId);
                window.Show();
            });
        }

        /// <summary>
        /// Переключает пользователя без сброса данных.
        /// </summary>
        public void SwitchUser()
        {
            Dispatcher.Invoke(() =>
            {
                var settingsRepo = new SettingsRepository(_dbContext);
                var appSettings = settingsRepo.GetAppSettings();

                var dialog = new SwitchUserWindow(_dbContext, appSettings.CurrentTeacherId);

                // Ищём окно-владельца
                var owner = Windows.OfType<MainWindow>().FirstOrDefault();
                if (owner != null) dialog.Owner = owner;

                if (dialog.ShowDialog() != true) return;

                // Сохраняем нового пользователя
                appSettings.CurrentTeacherId = dialog.SelectedTeacherId;
                appSettings.IsHead = dialog.SelectedIsHead;
                settingsRepo.SaveAppSettings(appSettings);

                // Обновляем IsHead у Teacher в БД
                var teacherRepo = new TeacherRepository(_dbContext);
                var teacher = teacherRepo.GetById(dialog.SelectedTeacherId);
                if (teacher != null)
                {
                    teacher.IsHead = dialog.SelectedIsHead;
                    teacherRepo.Update(teacher);
                }

                // Закрываем MainWindow и открываем заново
                foreach (var wnd in Windows.OfType<Window>().ToList())
                {
                    if (wnd is SwitchUserWindow) continue;
                    wnd.Tag = "FORCE_CLOSE";
                    wnd.Close();
                }

                ShowMainWindow();
            });
        }

        private void ExitApplication()
        {
            _trayIcon?.Dispose();
            _dbContext?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _dbContext?.Dispose();
            base.OnExit(e);
        }
    }
}