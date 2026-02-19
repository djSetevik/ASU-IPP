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
            ActionPanel.Visibility = Visibility.Collapsed;
            _currentViewTeacherId = null;

            _headVm = new HeadMainViewModel(_dbContext, _appSettings.CurrentTeacherId);
            _headVm.GoToTeacherWorkRequested += () => ShowSections(_appSettings.CurrentTeacherId);
            _headVm.TeacherSelected += teacherId => ShowTeacherSections(teacherId);
            _headVm.ImportRequested += DoImportArchives;

            var view = new HeadMainWindow { DataContext = _headVm };
            _nav.NavigateTo(view);
        }

        private void ShowTeacherSections(string teacherId)
        {
            _currentViewTeacherId = teacherId;
            NavPanel.Visibility = Visibility.Visible;
            ActionPanel.Visibility = Visibility.Visible;

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
            _currentViewTeacherId = teacherId;
            ActionPanel.Visibility = Visibility.Visible;

            var vm = new SectionsViewModel(_dbContext, teacherId);

            vm.SectionSelected += sectionId =>
            {
                ShowSectionDetail(teacherId, sectionId);
            };

            _nav.NavigateTo(new SectionsView { DataContext = vm });

            if (_appSettings.IsHead)
            {
                NavPanel.Visibility = Visibility.Visible;
            }
            else
            {
                NavPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSectionDetail(string teacherId, int sectionId)
        {
            _currentViewTeacherId = teacherId;
            ActionPanel.Visibility = Visibility.Visible;
            NavPanel.Visibility = Visibility.Visible;

            var refRepo = new ReferenceRepository(_dbContext);
            var section = refRepo.GetSection(sectionId);
            NavTitle.Text = $"{sectionId}. {section?.Name ?? ""}";

            var vm = new SectionDetailViewModel(_dbContext, teacherId, sectionId);

            // Добавить работу
            vm.AddWorkRequested += () =>
            {
                var allItems = refRepo.GetWorkItemsBySection(sectionId);
                // Убрали фильтрацию usedIds — пункт можно использовать несколько раз
                var picker = new WorkItemPickerWindow(allItems) { Owner = this };
                if (picker.ShowDialog() == true)
                    vm.AddWork(picker.SelectedWorkItem, picker.WorkName, picker.Points, picker.DueDate);
            };

            // Редактировать работу
            vm.EditWorkRequested += work =>
            {
                var workItem = refRepo.GetWorkItem(work.SectionId, work.ItemId);
                var allItems = refRepo.GetWorkItemsBySection(sectionId);
                // Все пункты доступны
                var editor = new EditWorkWindow(work, workItem, allItems) { Owner = this };
                if (editor.ShowDialog() == true)
                {
                    vm.UpdateWork(work.WorkId, editor.EditedWorkName,
                        editor.EditedPoints, editor.EditedDueDate, editor.EditedStatus,
                        editor.EditedWorkItem);
                }
            };

            var view = new SectionDetailView { DataContext = vm };
            _nav.NavigateTo(view);
        }

        // Текущий teacherId для отчётов
        private string _currentViewTeacherId;

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentViewTeacherId)) return;
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save report",
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    FileName = $"Report_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                var reportService = new Core.Services.ReportService(_dbContext);
                var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
                var filePath = reportService.GeneratePersonalReport(_currentViewTeacherId, dir);

                if (filePath != dialog.FileName && System.IO.File.Exists(filePath))
                {
                    if (System.IO.File.Exists(dialog.FileName)) System.IO.File.Delete(dialog.FileName);
                    System.IO.File.Move(filePath, dialog.FileName);
                    filePath = dialog.FileName;
                }

                var result = MessageBox.Show($"Saved:\n{filePath}\n\nOpen?", "ASUIPP", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "ASUIPP"); }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "ASUIPP_Manual.docx");

                if (!System.IO.File.Exists(helpPath))
                {
                    MessageBox.Show("Файл справки не найден.\nОжидается: " + helpPath, "АСУИПП");
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть справку: {ex.Message}", "АСУИПП");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentViewTeacherId)) return;
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save archive",
                    Filter = "ZIP (*.zip)|*.zip",
                    FileName = $"ASUIPP_{DateTime.Now:yyyy-MM-dd}.zip"
                };
                if (saveDialog.ShowDialog() != true) return;

                var dir = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                var exportService = new Core.Services.ExportService(
                    new Core.Data.Repositories.WorkRepository(_dbContext),
                    new Core.Data.Repositories.TeacherRepository(_dbContext),
                    new Core.Data.Repositories.SettingsRepository(_dbContext));
                var filePath = exportService.Export(_currentViewTeacherId, dir);

                if (filePath != saveDialog.FileName && System.IO.File.Exists(filePath))
                {
                    if (System.IO.File.Exists(saveDialog.FileName)) System.IO.File.Delete(saveDialog.FileName);
                    System.IO.File.Move(filePath, saveDialog.FileName);
                }

                MessageBox.Show($"Saved:\n{saveDialog.FileName}", "ASUIPP");
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "ASUIPP"); }
        }

        // ── Навигация ──

        private void GoBackToHead_Click(object sender, RoutedEventArgs e)
        {
            if (_appSettings.IsHead)
            {
                // Проверяем откуда пришли — из SectionDetail или из TeacherSections
                if (ContentArea.Content is SectionDetailView)
                {
                    // Вернуться к списку разделов
                    if (!string.IsNullOrEmpty(_currentViewTeacherId))
                    {
                        if (_currentViewTeacherId == _appSettings.CurrentTeacherId)
                            ShowSections(_currentViewTeacherId);
                        else
                            ShowTeacherSections(_currentViewTeacherId);
                    }
                }
                else
                {
                    // Вернуться к списку преподавателей
                    _headVm?.Refresh();
                    ShowHeadView();
                }
            }
            else
            {
                // Преподаватель — вернуться к разделам
                ShowSections(_appSettings.CurrentTeacherId);
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
            _headVm?.LoadTeachers();
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