using ASUIPP.Core.Data;
using System.Windows;

namespace ASUIPP.App.Views
{
    public partial class AddTeacherWindow : Window
    {
        private readonly DatabaseContext _dbContext;

        public string TeacherFullName { get; private set; }

        public AddTeacherWindow(DatabaseContext dbContext)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);
            _dbContext = dbContext;
            NameBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите ФИО преподавателя.", "АСУИПП");
                return;
            }

            var repo = new Core.Data.Repositories.TeacherRepository(_dbContext);
            if (repo.ExistsByFullName(NameBox.Text.Trim()))
            {
                MessageBox.Show($"Преподаватель \"{NameBox.Text.Trim()}\" уже существует.", "АСУИПП");
                return;
            }

            TeacherFullName = NameBox.Text.Trim();
            DialogResult = true;
        }
    }
}