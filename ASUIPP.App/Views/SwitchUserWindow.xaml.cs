using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;

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
            _currentTeacherId = currentTeacherId ?? "";  // null-safe
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
                    RoleText = t.IsHead ? "(zavkaf)" : "",
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

            InfoText.Text = $"Total: {_items.Count}";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersList.SelectedItem is UserListItem selected))
            {
                MessageBox.Show("Select user.", "ASUIPP");
                return;
            }

            bool wantHead = IsHeadCheckBox.IsChecked == true;

            // Only one head allowed
            if (wantHead && !selected.IsHead)
            {
                var existing = _items.FirstOrDefault(i => i.IsHead && i.TeacherId != selected.TeacherId);
                if (existing != null)
                {
                    MessageBox.Show(
                        $"Head is already assigned: {existing.FullName}.\nOnly one user can be head.",
                        "ASUIPP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            SelectedTeacherId = selected.TeacherId;
            SelectedIsHead = wantHead;
            DialogResult = true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddTeacherWindow(_dbContext);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var teacher = new Core.Models.Teacher
                {
                    TeacherId = System.Guid.NewGuid().ToString(),
                    FullName = dialog.TeacherFullName,
                    ShortName = Core.Helpers.NameHelper.ToShortName(dialog.TeacherFullName),
                    IsHead = false,
                    CreatedAt = System.DateTime.Now
                };
                _teacherRepo.Insert(teacher);

                _items.Add(new UserListItem
                {
                    TeacherId = teacher.TeacherId,
                    FullName = teacher.FullName,
                    IsHead = false,
                    RoleText = "",
                    IsCurrent = false
                });

                UsersList.SelectedItem = _items.Last();
                InfoText.Text = $"Added. Total: {_items.Count}";
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersList.SelectedItem is UserListItem selected))
            {
                MessageBox.Show("Select user to delete.", "ASUIPP");
                return;
            }

            if (!string.IsNullOrEmpty(_currentTeacherId) && selected.TeacherId == _currentTeacherId)
            {
                MessageBox.Show("Cannot delete current user.", "ASUIPP");
                return;
            }

            if (_items.Count <= 1)
            {
                MessageBox.Show("Cannot delete last user.", "ASUIPP");
                return;
            }

            var worksCount = _workRepo.GetByTeacher(selected.TeacherId).Count;
            var msg = $"Delete user \"{selected.FullName}\"?";
            if (worksCount > 0)
                msg += $"\n\nAll works ({worksCount}) and files will be deleted.";

            var result = MessageBox.Show(msg, "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var works = _workRepo.GetByTeacher(selected.TeacherId);
            foreach (var work in works)
            {
                Core.Helpers.FileHelper.DeleteWorkFiles(selected.TeacherId, work.WorkId);
            }

            _teacherRepo.Delete(selected.TeacherId);
            _items.Remove(selected);
            InfoText.Text = $"Deleted. Remaining: {_items.Count}";
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