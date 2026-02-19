using System;
using System.Windows;
using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;
using ASUIPP.Core.Services;

namespace ASUIPP.App.ViewModels
{
    public class SetupViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly SettingsRepository _settingsRepo;
        private readonly TeacherRepository _teacherRepo;

        // ── Привязки ──

        private string _excelFilePath;
        public string ExcelFilePath
        {
            get => _excelFilePath;
            set
            {
                if (SetProperty(ref _excelFilePath, value))
                    OnPropertyChanged(nameof(CanFinish));
            }
        }

        private string _statusText = "Загрузите Excel-файл с показателями";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private ExcelImportService.ImportResult _importResult;

        public bool CanFinish => _importResult != null && _importResult.SectionsCount > 0;

        // ── Команды ──

        public RelayCommand BrowseExcelCommand { get; }
        public RelayCommand FinishCommand { get; }

        // ── События ──

        public event Action SetupCompleted;

        // ── Конструктор ──

        public SetupViewModel(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _settingsRepo = new SettingsRepository(dbContext);
            _teacherRepo = new TeacherRepository(dbContext);

            BrowseExcelCommand = new RelayCommand(BrowseExcel);
            FinishCommand = new RelayCommand(Finish, () => CanFinish);
        }

        private void BrowseExcel()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл с показателями",
                Filter = "Excel файлы (*.xls;*.xlsx)|*.xls;*.xlsx",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                StatusText = "Загрузка и импорт...";

                var importService = new ExcelImportService(_dbContext);
                _importResult = importService.ImportAll(dialog.FileName);

                ExcelFilePath = dialog.FileName;

                var msg = $"Разделов: {_importResult.SectionsCount}, " +
                          $"пунктов: {_importResult.WorkItemsCount}, " +
                          $"преподавателей: {_importResult.TeachersCount}, " +
                          $"баллов: {_importResult.ScoresCount}";

                if (!string.IsNullOrEmpty(_importResult.Year))
                    msg += $"\nУчебный год: {_importResult.Year}, семестр {_importResult.Semester}";

                if (_importResult.Errors.Count > 0)
                    msg += $"\nОшибок: {_importResult.Errors.Count}";

                StatusText = msg;

                OnPropertyChanged(nameof(CanFinish));
                FinishCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
                _importResult = null;
                ExcelFilePath = null;
            }
        }

        private void Finish()
        {
            try
            {
                var teachers = _teacherRepo.GetAll();

                if (teachers.Count == 0)
                {
                    MessageBox.Show("Преподаватели не найдены в таблице.", "АСУИПП");
                    return;
                }

                // Открываем окно выбора пользователя
                var switchWindow = new Views.SwitchUserWindow(_dbContext, null);
                if (switchWindow.ShowDialog() == true)
                {
                    // Сохраняем настройки
                    _settingsRepo.Set("CurrentTeacherId", switchWindow.SelectedTeacherId);
                    _settingsRepo.Set("IsHead", switchWindow.SelectedIsHead ? "1" : "0");
                    _settingsRepo.Set("IsFirstRun", "0");
                    _settingsRepo.Set("ReferenceFilePath", ExcelFilePath ?? "");

                    if (!string.IsNullOrEmpty(_importResult?.Year))
                        _settingsRepo.Set("SemesterYear", _importResult.Year);
                    if (!string.IsNullOrEmpty(_importResult?.Semester))
                        _settingsRepo.Set("SemesterNumber", _importResult.Semester);

                    // Обновляем IsHead у выбранного преподавателя
                    var teacher = _teacherRepo.GetById(switchWindow.SelectedTeacherId);
                    if (teacher != null)
                    {
                        // Сначала снимаем IsHead у всех
                        foreach (var t in _teacherRepo.GetAll())
                        {
                            if (t.IsHead)
                            {
                                t.IsHead = false;
                                _teacherRepo.Update(t);
                            }
                        }

                        teacher.IsHead = switchWindow.SelectedIsHead;
                        _teacherRepo.Update(teacher);
                    }

                    SetupCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
        }
    }
}