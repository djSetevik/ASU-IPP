using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using ASUIPP.Core.Services;

namespace ASUIPP.App.ViewModels
{
    public class SetupViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly SettingsRepository _settingsRepo;
        private readonly TeacherRepository _teacherRepo;
        private ExcelImportService _importService;

        // ── Привязки ──

        private string _excelFilePath;
        public string ExcelFilePath
        {
            get => _excelFilePath;
            set
            {
                if (SetProperty(ref _excelFilePath, value))
                    OnPropertyChanged(nameof(IsExcelLoaded));
            }
        }

        public bool IsExcelLoaded => !string.IsNullOrEmpty(ExcelFilePath) && _departments.Count > 0;

        private string _statusText = "Загрузите Excel-файл с показателями";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private int _importedItemsCount;
        public int ImportedItemsCount
        {
            get => _importedItemsCount;
            set => SetProperty(ref _importedItemsCount, value);
        }

        // Кафедры
        private ObservableCollection<DepartmentInfo> _departments = new ObservableCollection<DepartmentInfo>();
        public ObservableCollection<DepartmentInfo> Departments => _departments;

        private DepartmentInfo _selectedDepartment;
        public DepartmentInfo SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                    OnPropertyChanged(nameof(CanFinish));
            }
        }

        // ФИО
        private string _fullName = "";
        public string FullName
        {
            get => _fullName;
            set
            {
                if (SetProperty(ref _fullName, value))
                    OnPropertyChanged(nameof(CanFinish));
            }
        }

        // Роль
        private bool _isHead;
        public bool IsHead
        {
            get => _isHead;
            set => SetProperty(ref _isHead, value);
        }

        // Семестр (из Excel)
        private string _semesterDisplay = "";
        public string SemesterDisplay
        {
            get => _semesterDisplay;
            set => SetProperty(ref _semesterDisplay, value);
        }

        private SemesterInfo _semesterInfo;

        public bool CanFinish =>
            IsExcelLoaded &&
            !string.IsNullOrWhiteSpace(FullName) &&
            SelectedDepartment != null;

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
            _importService = new ExcelImportService(dbContext);

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
                StatusText = "Загрузка справочника...";

                // Парсим справочник работ
                var count = _importService.ImportReference(dialog.FileName);
                ImportedItemsCount = count;

                // Парсим кафедры
                var deps = _importService.ImportDepartments(dialog.FileName);
                _departments.Clear();
                foreach (var d in deps)
                    _departments.Add(d);

                // Парсим семестр
                _semesterInfo = _importService.ParseSemesterInfo(dialog.FileName);
                SemesterDisplay = $"{_semesterInfo.Number}-й семестр {_semesterInfo.Year} уч. года";

                ExcelFilePath = dialog.FileName;
                StatusText = $"Загружено {count} пунктов работ, {deps.Count} кафедр";

                OnPropertyChanged(nameof(IsExcelLoaded));
                OnPropertyChanged(nameof(CanFinish));
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
                ExcelFilePath = null;
            }
        }

        private void Finish()
        {
            try
            {
                // Создаём преподавателя
                var teacher = new Teacher
                {
                    FullName = FullName.Trim(),
                    ShortName = FileHelper.ToShortName(FullName),
                    IsHead = IsHead
                };
                _teacherRepo.Insert(teacher);

                // Сохраняем настройки
                var settings = new AppSettings
                {
                    CurrentTeacherId = teacher.TeacherId,
                    IsHead = IsHead,
                    DepartmentName = SelectedDepartment.FullName,
                    DepartmentShortName = SelectedDepartment.ShortName,
                    SemesterYear = _semesterInfo?.Year ?? "",
                    SemesterNumber = _semesterInfo?.Number ?? 0,
                    IsFirstRun = false,
                    ReferenceFilePath = ExcelFilePath
                };
                _settingsRepo.SaveAppSettings(settings);

                SetupCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка сохранения: {ex.Message}";
            }
        }
    }
}