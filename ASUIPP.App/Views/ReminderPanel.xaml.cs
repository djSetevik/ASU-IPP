using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASUIPP.App.ViewModels;
using ASUIPP.Core.Data;

namespace ASUIPP.App.Views
{
    public partial class ReminderWindow : Window
    {
        private readonly ReminderViewModel _viewModel;

        public ReminderWindow(DatabaseContext dbContext, string teacherId)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);
            _viewModel = new ReminderViewModel(dbContext, teacherId);
            DataContext = _viewModel;

            // Растягиваем на всю высоту экрана, прижимаем вправо
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width;
            Top = screen.Top;
            Height = screen.Height;
        }

        private void Item_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ReminderItem item)
            {
                // Открываем основное окно на нужном разделе
                var app = Application.Current as App;
                app?.ShowMainWindow();
                // TODO: навигация к конкретному разделу
            }
        }
    }
}