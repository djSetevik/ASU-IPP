using System;
using System.Windows;
using System.Windows.Media;

namespace ASUIPP.App.Helpers
{
    public static class ZoomHelper
    {
        private static double _zoom = 1.0;

        public static double Zoom
        {
            get => _zoom;
            set => _zoom = Math.Max(0.7, Math.Min(1.5, value));
        }

        /// <summary>
        /// Применяет текущий масштаб к окну.
        /// Вызывать в конструкторе после InitializeComponent().
        /// </summary>
        public static void Apply(Window window)
        {
            if (window.Content is FrameworkElement root)
            {
                root.LayoutTransform = new ScaleTransform(_zoom, _zoom);
            }
        }

        /// <summary>
        /// Применяет масштаб ко всем открытым окнам.
        /// </summary>
        public static void ApplyToAll()
        {
            foreach (Window wnd in Application.Current.Windows)
            {
                Apply(wnd);
            }
        }
    }
}