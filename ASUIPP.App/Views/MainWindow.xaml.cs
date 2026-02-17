using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ASUIPP.App.Helpers;
using ASUIPP.App.ViewModels;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseContext _dbContext;
        private readonly AppSettings _appSettings;
        private readonly NavigationService _nav;

        private SectionsViewModel _sectionsVm;
        private HeadMainViewModel _headVm;
        private double _zoomLevel = 1.0;
        private ScaleTransform _scaleTransform;

        public MainWindow(DatabaseContext dbContext, AppSettings appSettings)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);

            _dbContext = dbContext;
            _appSettings = appSettings;
            _nav = new NavigationService(ContentArea);

            TitleText.Text = $"АСУИПП — {(_appSettings.IsHead ? "Заведующий кафедрой" : "Преподаватель")}";
            AutoStartMenuItem.IsChecked = Helpers.AutoStartHelper.IsEnabled();

            LoadZoomLevel();

            if (_appSettings.IsHead)
                ShowHeadView();
            else
                ShowSections(_appSettings.CurrentTeacherId);
        }

        // ── Кастомный заголовок ──

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                if (e.ClickCount == 2)
                    ToggleMaximize();
                else
                    DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaxBtn.Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaxBtn.Content = "❐";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Hide();

        private void Hide_Click(object sender, RoutedEventArgs e)
            => Hide();

        // ── Масштаб ──

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
            => SetZoom(_zoomLevel + 0.1);

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
            => SetZoom(_zoomLevel - 0.1);

        private void SetZoom(double level)
        {
            ZoomHelper.Zoom = level;
            _zoomLevel = ZoomHelper.Zoom;

            // Применяем ко всем окнам
            ZoomHelper.ApplyToAll();

            ZoomLabel.Text = $"{(int)(_zoomLevel * 100)}%";

            try
            {
                var settingsRepo = new SettingsRepository(_dbContext);
                settingsRepo.Set("ZoomLevel",
                    _zoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private void LoadZoomLevel()
        {
            try
            {
                var settingsRepo = new SettingsRepository(_dbContext);
                var saved = settingsRepo.Get("ZoomLevel");
                if (!string.IsNullOrEmpty(saved) &&
                    double.TryParse(saved, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var level))
                {
                    SetZoom(level);
                    return;
                }
            }
            catch { }
            SetZoom(1.0);
        }

        // ── Режим завкафедрой ──

        private void ShowHeadView()
        {
            NavPanel.Visibility = Visibility.Collapsed;

            _headVm = new HeadMainViewModel(_dbContext, _appSettings.CurrentTeacherId);
            _headVm.GoToTeacherWorkRequested += () => ShowSections(_appSettings.CurrentTeacherId);
            _headVm.TeacherSelected += teacherId => ShowTeacherSections(teacherId);
            _headVm.ImportRequested += DoImportArchives;

            var view = new HeadMainWindow { DataContext = _headVm };
            _nav.NavigateTo(view);
        }

        private void ShowTeacherSections(string teacherId)
        {
            var teacherRepo = new TeacherRepository(_dbContext);
            var teacher = teacherRepo.GetById(teacherId);

            NavPanel.Visibility = Visibility.Visible;
            NavTitle.Text = $"Работы: {teacher?.ShortName ?? "Преподаватель"}";

            _sectionsVm = new SectionsViewModel(_dbContext, teacherId);
            _sectionsVm.SectionSelected += sectionId => ShowSectionDetail(teacherId, sectionId);

            var view = new SectionsView { DataContext = _sectionsVm };
            _nav.NavigateTo(view);
        }

        // ── Режим преподавателя ──

        private void ShowSections(string teacherId)
        {
            if (_appSettings.IsHead)
            {
                NavPanel.Visibility = Visibility.Visible;
                NavTitle.Text = "Мои работы";
            }
            else
            {
                NavPanel.Visibility = Visibility.Collapsed;
            }

            _sectionsVm = new SectionsViewModel(_dbContext, teacherId);
            _sectionsVm.SectionSelected += sectionId => ShowSectionDetail(teacherId, sectionId);

            var view = new SectionsView { DataContext = _sectionsVm };
            _nav.NavigateTo(view);
        }

        private void ShowSectionDetail(string teacherId, int sectionId)
        {
            var refRepo = new ReferenceRepository(_dbContext);
            var vm = new SectionDetailViewModel(_dbContext, teacherId, sectionId);

            vm.GoBackRequested += () =>
            {
                _sectionsVm?.Refresh();
                _headVm?.Refresh();

                if (teacherId == _appSettings.CurrentTeacherId && !_appSettings.IsHead)
                    ShowSections(teacherId);
                else
                    ShowTeacherSections(teacherId);
            };

            vm.AddWorkRequested += () =>
            {
                var workItems = refRepo.GetWorkItemsBySection(sectionId);
                var picker = new WorkItemPickerWindow(workItems) { Owner = this };
                if (picker.ShowDialog() == true)
                    vm.AddWork(picker.SelectedWorkItem, picker.WorkName, picker.Points, picker.DueDate);
            };

            vm.EditWorkRequested += work =>
            {
                var workItem = refRepo.GetWorkItem(work.SectionId, work.ItemId);
                var allItems = refRepo.GetWorkItemsBySection(sectionId);
                var editor = new EditWorkWindow(work, workItem, allItems) { Owner = this };
                if (editor.ShowDialog() == true)
                {
                    vm.UpdateWork(work.WorkId, editor.EditedWorkName,
                        editor.EditedPoints, editor.EditedDueDate, editor.EditedStatus,
                        editor.EditedWorkItem);
                }
            };

            var sections = refRepo.GetAllSections();
            var sec = sections.FirstOrDefault(s => s.SectionId == sectionId);
            NavPanel.Visibility = Visibility.Visible;
            NavTitle.Text = sec != null ? $"{sec.SectionId}. {sec.Name}" : "Раздел";

            var view = new SectionDetailView { DataContext = vm };
            _nav.NavigateTo(view);
        }

        // ── Навигация ──

        private void GoBackToHead_Click(object sender, RoutedEventArgs e)
        {
            if (_appSettings.IsHead)
            {
                _headVm?.Refresh();
                ShowHeadView();
            }
        }

        // ── Импорт архивов ──

        private void DoImportArchives()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите архивы преподавателей",
                Filter = "ZIP-архивы (*.zip)|*.zip",
                Multiselect = true
            };
            if (dialog.ShowDialog() != true) return;

            int imported = 0, errors = 0;
            foreach (var zipPath in dialog.FileNames)
            {
                try
                {
                    new Core.Services.ImportService(_dbContext).Import(zipPath);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors++;
                    System.Diagnostics.Debug.WriteLine($"Import error: {ex.Message}");
                }
            }

            var msg = $"Импортировано архивов: {imported}";
            if (errors > 0) msg += $"\nОшибок: {errors}";
            MessageBox.Show(msg, "Импорт завершён");
            _headVm?.LoadData();
        }

        // ── Меню ──

        private void AutoStart_Click(object sender, RoutedEventArgs e)
            => Helpers.AutoStartHelper.SetEnabled(AutoStartMenuItem.IsChecked);

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;
            app?.SwitchUser();
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Это удалит ВСЕ данные и настройки.\nПродолжить?",
                "АСУИПП — Сброс", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbPath = DatabaseContext.DefaultDbPath;
                _dbContext.Dispose();
                if (System.IO.File.Exists(dbPath))
                    System.IO.File.Delete(dbPath);

                var filesDir = Core.Helpers.FileHelper.FilesRoot;
                if (System.IO.Directory.Exists(filesDir))
                    System.IO.Directory.Delete(filesDir, true);

                MessageBox.Show("Настройки сброшены. Приложение будет перезапущено.", "АСУИПП");
                System.Diagnostics.Process.Start(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "АСУИПП");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Tag is string s && s == "FORCE_CLOSE")
            {
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
            Hide();
        }
    }
}