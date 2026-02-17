using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASUIPP.App.ViewModels
{
    public class StatusOption
    {
        public WorkStatus Value { get; set; }
        public string DisplayName { get; set; }
    }

    public class SectionDetailViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string _teacherId;
        private readonly int _sectionId;
        private readonly WorkRepository _workRepo;
        private readonly ReferenceRepository _refRepo;

        // Добавьте в начало класса вместе с остальными свойствами:

        public List<StatusOption> StatusOptions { get; } = new List<StatusOption>
        {
            new StatusOption { Value = WorkStatus.Planned, DisplayName = "Запланирована" },
            new StatusOption { Value = WorkStatus.InProgress, DisplayName = "Выполняется" },
            new StatusOption { Value = WorkStatus.Done, DisplayName = "Ожидает подтверждения" },
            new StatusOption { Value = WorkStatus.Confirmed, DisplayName = "Подтверждена" },
            new StatusOption { Value = WorkStatus.Reported, DisplayName = "Учтена в отчёте" }
        };

        // Публичный метод для вызова из code-behind:
        public void ChangeWorkStatus(string workId, WorkStatus newStatus)
        {
            _workRepo.UpdateStatus(workId, newStatus);
            LoadData();
        }

        private string _sectionName;
        public string SectionName
        {
            get => _sectionName;
            set => SetProperty(ref _sectionName, value);
        }

        private int _sectionPoints;
        public int SectionPoints
        {
            get => _sectionPoints;
            set => SetProperty(ref _sectionPoints, value);
        }

        private int _grandTotal;
        public int GrandTotal
        {
            get => _grandTotal;
            set => SetProperty(ref _grandTotal, value);
        }

        public ObservableCollection<PlannedWork> Works { get; }
            = new ObservableCollection<PlannedWork>();

        public RelayCommand AddWorkCommand { get; }
        public RelayCommand<PlannedWork> AttachFileCommand { get; }
        public RelayCommand<PlannedWork> DeleteWorkCommand { get; }
        public RelayCommand<PlannedWork> OpenFilesCommand { get; }
        public RelayCommand CreateReportCommand { get; }
        public RelayCommand GoBackCommand { get; }
        public RelayCommand<PlannedWork> ChangeStatusCommand { get; }

        public RelayCommand<PlannedWork> EditWorkCommand { get; }

        public event Action<PlannedWork> EditWorkRequested;

        public event Action GoBackRequested;
        public event Action AddWorkRequested;

        public SectionDetailViewModel(DatabaseContext dbContext, string teacherId, int sectionId)
        {
            _dbContext = dbContext;
            _teacherId = teacherId;
            _sectionId = sectionId;
            _workRepo = new WorkRepository(dbContext);
            _refRepo = new ReferenceRepository(dbContext);

            AttachFileCommand = new RelayCommand<PlannedWork>(work =>
            {
                if (work == null) return;

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Выберите подтверждающий документ",
                    Filter = "Изображения и документы|*.jpg;*.jpeg;*.png;*.pdf|Все файлы|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog() != true) return;

                foreach (var filePath in dialog.FileNames)
                {
                    var relativePath = FileHelper.CopyFileToStorage(filePath, _teacherId, work.WorkId);
                    var af = new AttachedFile
                    {
                        WorkId = work.WorkId,
                        FileName = System.IO.Path.GetFileName(relativePath),
                        FilePath = relativePath,
                        FileType = FileHelper.GetFileType(filePath)
                    };
                    _workRepo.InsertFile(af);
                }

                LoadData();
            });

            ChangeStatusCommand = new RelayCommand<PlannedWork>(work =>
            {
                if (work == null) return;

                // Циклически переключаем статус zaeb
                WorkStatus next;
                switch (work.Status)
                {
                    case WorkStatus.Planned: next = WorkStatus.InProgress; break;
                    case WorkStatus.InProgress: next = WorkStatus.Done; break;
                    case WorkStatus.Done: next = WorkStatus.Confirmed; break;
                    case WorkStatus.Confirmed: next = WorkStatus.Reported; break;
                    default: next = WorkStatus.Planned; break;
                }

                _workRepo.UpdateStatus(work.WorkId, next);
                LoadData();
            });

            AddWorkCommand = new RelayCommand(() => AddWorkRequested?.Invoke());

            DeleteWorkCommand = new RelayCommand<PlannedWork>(work =>
            {
                if (work == null) return;
                var result = System.Windows.MessageBox.Show(
                    $"Удалить работу \"{work.WorkName}\"?",
                    "Подтверждение", System.Windows.MessageBoxButton.YesNo);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    FileHelper.DeleteWorkFiles(_teacherId, work.WorkId);
                    _workRepo.Delete(work.WorkId);
                    LoadData();
                }
            });

            OpenFilesCommand = new RelayCommand<PlannedWork>(work =>
            {
                if (work == null) return;
                var dir = FileHelper.GetWorkFilesDir(_teacherId, work.WorkId);
                if (System.IO.Directory.Exists(dir))
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            });

            CreateReportCommand = new RelayCommand(() =>
            {
                System.Windows.MessageBox.Show("Генерация отчёта — в разработке", "АСУИПП");
            });

            EditWorkCommand = new RelayCommand<PlannedWork>(work =>
            {
                if (work != null)
                    EditWorkRequested?.Invoke(work);
            });

            GoBackCommand = new RelayCommand(() => GoBackRequested?.Invoke());

            LoadData();
        }

        public void LoadData()
        {
            var sections = _refRepo.GetAllSections();
            var section = sections.FirstOrDefault(s => s.SectionId == _sectionId);
            SectionName = section != null ? $"{section.SectionId}. {section.Name}" : "Раздел";

            var works = _workRepo.GetByTeacherAndSection(_teacherId, _sectionId);

            // Подгружаем WorkItem для отображения названия пункта
            foreach (var work in works)
            {
                work.WorkItem = _refRepo.GetWorkItem(work.SectionId, work.ItemId);
            }

            Works.Clear();
            foreach (var w in works)
                Works.Add(w);

            SectionPoints = Works.Sum(w => w.Points);
            GrandTotal = _workRepo.GetTotalPointsByTeacher(_teacherId);
        }

        public void AddWork(WorkItem selectedItem, string workName, int points, DateTime? dueDate)
        {
            var work = new PlannedWork
            {
                TeacherId = _teacherId,
                SectionId = _sectionId,
                ItemId = selectedItem.ItemId,
                WorkName = workName,
                Points = points,
                DueDate = dueDate,
                Status = WorkStatus.Planned
            };

            _workRepo.Insert(work);
            LoadData();
        }
        public void UpdateWork(string workId, string workName, int points,
            DateTime? dueDate, WorkStatus status, WorkItem newItem)
        {
            var work = _workRepo.GetById(workId);
            if (work == null) return;

            work.WorkName = workName;
            work.Points = points;
            work.DueDate = dueDate;
            work.Status = status;

            if (newItem != null)
            {
                work.ItemId = newItem.ItemId;
                // Не меняем SectionId — пункт в рамках текущего раздела
            }

            _workRepo.Update(work);
            LoadData();
        }
    }

    /// <summary>
    /// RelayCommand с типизированным параметром.
    /// </summary>
    public class RelayCommand<T> : System.Windows.Input.ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => System.Windows.Input.CommandManager.RequerySuggested += value;
            remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
        }


        public bool CanExecute(object parameter) =>
            _canExecute == null || (parameter is T t && _canExecute(t));

        public void Execute(object parameter)
        {
            if (parameter is T t)
                _execute(t);
        }
    }
}