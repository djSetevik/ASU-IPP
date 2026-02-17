using System.Windows;

namespace ASUIPP.App.Views
{
    public partial class AddTeacherWindow : Window
    {
        public string TeacherFullName { get; private set; }

        public AddTeacherWindow()
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);
            NameBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите ФИО преподавателя.", "АСУИПП");
                return;
            }
            TeacherFullName = NameBox.Text.Trim();
            DialogResult = true;
        }
    }
}