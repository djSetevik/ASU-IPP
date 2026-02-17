using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace ASUIPP.App.Helpers
{
    /// <summary>
    /// Управляет иконкой в системном трее и её контекстным меню.
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private TaskbarIcon _trayIcon;

        public event Action OpenRequested;
        public event Action ReminderRequested;
        public event Action ExitRequested;

        public void Initialize()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "АСУИПП — Система учёта показателей",
                MenuActivation = PopupActivationMode.RightClick
            };

            var menu = new ContextMenu();

            var openItem = new MenuItem { Header = "Открыть АСУИПП" };
            openItem.Click += (s, e) => OpenRequested?.Invoke();
            menu.Items.Add(openItem);

            var reminderItem = new MenuItem { Header = "Напоминания" };
            reminderItem.Click += (s, e) => ReminderRequested?.Invoke();
            menu.Items.Add(reminderItem);

            menu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Выход" };
            exitItem.Click += (s, e) => ExitRequested?.Invoke();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
            _trayIcon.TrayMouseDoubleClick += (s, e) => OpenRequested?.Invoke();
            _trayIcon.TrayLeftMouseUp += (s, e) => ReminderRequested?.Invoke();
        }

        /// <summary>
        /// Показывает всплывающее уведомление из трея.
        /// </summary>
        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(title, message, icon);
        }

        public void Dispose()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
    }
}