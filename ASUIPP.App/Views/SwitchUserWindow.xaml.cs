using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Views
{
    public partial class SwitchUserWindow : Window
    {
        private readonly DatabaseContext _dbContext;
        private readonly string _currentTeacherId;
        private readonly TeacherRepository _teacherRepo;
        private readonly WorkRepository _workRepo;
        private ObservableCollection<UserListItem> _items;

        public string SelectedTeacherId { get; private set; }
        public bool SelectedIsHead { get; private set; }

        public SwitchUserWindow(DatabaseContext dbContext, string currentTeacherId)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);
            _dbContext = dbContext;
            _currentTeacherId = currentTeacherId;
            _teacherRepo = new TeacherRepository(dbContext);
            _workRepo = new WorkRepository(dbContext);

            LoadUsers();
        }

        private void LoadUsers()
        {
            var teachers = _teacherRepo.GetAll();

            _items = new ObservableCollection<UserListItem>(
                teachers.Select(t => new UserListItem
                {
                    TeacherId = t.TeacherId,
                    FullName = t.FullName,
                    IsHead = t.IsHead,
                    RoleText = t.IsHead ? "(завкафедрой)" : "",
                    IsCurrent = t.TeacherId == _currentTeacherId
                })
            );

            UsersList.ItemsSource = _items;

            var current = _items.FirstOrDefault(i => i.IsCurrent);
            if (current != null)
            {
                UsersList.SelectedItem = current;
                IsHeadCheckBox.IsChecked = current.IsHead;
            }

            InfoText.Text = $"Всего пользователей: {_items.Count}";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersList.SelectedItem is UserListItem selected))
            {
                MessageBox.Show("Выберите пользователя.", "АСУИПП");
                return;
            }

            SelectedTeacherId = selected.TeacherId;
            SelectedIsHead = IsHeadCheckBox.IsChecked == true;
            DialogResult = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersList.SelectedItem is UserListItem selected))
            {
                MessageBox.Show("Выберите пользователя для удаления.", "АСУИПП");
                return;
            }

            if (selected.TeacherId == _currentTeacherId)
            {
                MessageBox.Show("Нельзя удалить текущего пользователя.\nСначала переключитесь на другого.",
                    "АСУИПП", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_items.Count <= 1)
            {
                MessageBox.Show("Нельзя удалить последнего пользователя.", "АСУИПП");
                return;
            }

            var worksCount = _workRepo.GetByTeacher(selected.TeacherId).Count;
            var msg = $"Удалить пользователя \"{selected.FullName}\"?";
            if (worksCount > 0)
                msg += $"\n\nБудут также удалены все его работы ({worksCount} шт.) и прикреплённые файлы.";

            var result = MessageBox.Show(msg, "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Удаляем файлы с диска
            var works = _workRepo.GetByTeacher(selected.TeacherId);
            foreach (var work in works)
            {
                FileHelper.DeleteWorkFiles(selected.TeacherId, work.WorkId);
            }

            // Удаляем из БД (каскадно удалятся PlannedWorks и AttachedFiles)
            _teacherRepo.Delete(selected.TeacherId);

            _items.Remove(selected);
            InfoText.Text = $"Пользователь удалён. Осталось: {_items.Count}";
        }

        public class UserListItem
        {
            public string TeacherId { get; set; }
            public string FullName { get; set; }
            public bool IsHead { get; set; }
            public string RoleText { get; set; }
            public bool IsCurrent { get; set; }
        }
    }
}