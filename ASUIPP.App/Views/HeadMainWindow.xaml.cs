using System.Windows;
using System.Windows.Controls;

namespace ASUIPP.App.Views
{
    public partial class HeadMainWindow : UserControl
    {
        public HeadMainWindow()
        {
            InitializeComponent();
        }

        private void TeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string teacherId)
            {
                var vm = DataContext as ViewModels.HeadMainViewModel;
                vm?.SelectTeacher(teacherId);
            }
        }
    }
}