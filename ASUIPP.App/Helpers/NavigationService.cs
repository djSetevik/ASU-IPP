using System;
using System.Windows.Controls;

namespace ASUIPP.App.Helpers
{
    /// <summary>
    /// Простая навигация между UserControl'ами внутри MainWindow.
    /// MainWindow содержит ContentControl, в который подставляются View.
    /// </summary>
    public class NavigationService
    {
        private readonly ContentControl _container;

        public event Action<UserControl> Navigated;

        public UserControl CurrentView { get; private set; }

        public NavigationService(ContentControl container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public void NavigateTo(UserControl view)
        {
            CurrentView = view;
            _container.Content = view;
            Navigated?.Invoke(view);
        }

        /// <summary>
        /// Стек для кнопки «Назад». Простой вариант без полного фреймворка.
        /// </summary>
        private readonly System.Collections.Generic.Stack<UserControl> _history
            = new System.Collections.Generic.Stack<UserControl>();

        public void NavigateToWithHistory(UserControl view)
        {
            if (CurrentView != null)
                _history.Push(CurrentView);

            NavigateTo(view);
        }

        public bool CanGoBack => _history.Count > 0;

        public void GoBack()
        {
            if (_history.Count > 0)
            {
                var previous = _history.Pop();
                NavigateTo(previous);
            }
        }
    }
}