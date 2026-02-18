using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;

namespace ASUIPP.App.ViewModels
{
    public class TeacherListItem : ViewModelBase
    {
        public string TeacherId { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public bool IsHead { get; set; }

        private int _totalPoints;
        public int TotalPoints
        {
            get => _totalPoints;
            set => SetProperty(ref _totalPoints, value);
        }
    }

    public class HeadMainViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string _currentTeacherId;
        private readonly TeacherRepository _teacherRepo;
        private readonly WorkRepository _workRepo;

        public ObservableCollection<TeacherListItem> Teachers { get; }
            = new ObservableCollection<TeacherListItem>();

        private int _grandTotal;
        public int GrandTotal
        {
            get => _grandTotal;
            set => SetProperty(ref _grandTotal, value);
        }

        private string _welcomeName;
        public string WelcomeName
        {
            get => _welcomeName;
            set => SetProperty(ref _welcomeName, value);
        }

        public RelayCommand GoToTeacherWorkCommand { get; }
        public RelayCommand CreateSummaryReportCommand { get; }
        public RelayCommand ImportArchivesCommand { get; }
        public RelayCommand AddTeacherCommand { get; }

        public event Action GoToTeacherWorkRequested;
        public event Action<string> TeacherSelected;
        public event Action ImportRequested;

        public HeadMainViewModel(DatabaseContext dbContext, string currentTeacherId)
        {
            _dbContext = dbContext;
            _currentTeacherId = currentTeacherId;
            _teacherRepo = new TeacherRepository(dbContext);
            _workRepo = new WorkRepository(dbContext);

            var current = _teacherRepo.GetById(_currentTeacherId);
            WelcomeName = current?.FullName ?? current?.ShortName ?? "";

            GoToTeacherWorkCommand = new RelayCommand(() => GoToTeacherWorkRequested?.Invoke());

            CreateSummaryReportCommand = new RelayCommand(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Сохранить сводный отчёт",
                        Filter = "Excel (*.xlsx)|*.xlsx",
                        FileName = $"Сводный_отчёт_{DateTime.Now:yyyy-MM-dd}.xlsx"
                    };
                    if (dialog.ShowDialog() != true) return;

                    var reportService = new Core.Services.ReportService(_dbContext);
                    var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
                    var filePath = reportService.GenerateSummaryReport(dir);

                    if (filePath != dialog.FileName && System.IO.File.Exists(filePath))
                    {
                        if (System.IO.File.Exists(dialog.FileName))
                            System.IO.File.Delete(dialog.FileName);
                        System.IO.File.Move(filePath, dialog.FileName);
                        filePath = dialog.FileName;
                    }

                    var result = System.Windows.MessageBox.Show(
                        $"Сводный отчёт сохранён:\n{filePath}\n\nОткрыть?",
                        "АСУИПП", System.Windows.MessageBoxButton.YesNo);
                    if (result == System.Windows.MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "АСУИПП");
                }
            });

            ImportArchivesCommand = new RelayCommand(() => ImportRequested?.Invoke());
            AddTeacherCommand = new RelayCommand(AddTeacher);

            LoadTeachers();
        }

        public void LoadTeachers()
        {
            var teachers = _teacherRepo.GetAll();
            var refRepo = new ReferenceRepository(_dbContext);
            var sections = refRepo.GetAllSections();

            Teachers.Clear();
            int grandTotal = 0;

            foreach (var t in teachers)
            {
                var works = _workRepo.GetByTeacher(t.TeacherId);

                int total = 0;
                foreach (var sec in sections)
                {
                    var secSum = works.Where(w => w.SectionId == sec.SectionId).Sum(w => w.Points);
                    total += Math.Min(secSum, PointsLimits.MaxPerSection);
                }
                total = Math.Min(total, PointsLimits.MaxTotal);

                Teachers.Add(new TeacherListItem
                {
                    TeacherId = t.TeacherId,
                    FullName = t.FullName,
                    ShortName = t.ShortName,
                    IsHead = t.IsHead,
                    TotalPoints = total
                });

                grandTotal += total;
            }

            GrandTotal = grandTotal;
        }

        public void Refresh()
        {
            LoadTeachers();
        }

        private void AddTeacher()
        {
            var dialog = new Views.AddTeacherWindow(_dbContext) { Owner = App.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                var teacher = new Teacher
                {
                    TeacherId = Guid.NewGuid().ToString(),
                    FullName = dialog.TeacherFullName,
                    ShortName = NameHelper.ToShortName(dialog.TeacherFullName),
                    IsHead = false,
                    CreatedAt = DateTime.Now
                };
                _teacherRepo.Insert(teacher);
                LoadTeachers();
            }
        }

        public void SelectTeacher(string teacherId)
        {
            TeacherSelected?.Invoke(teacherId);
        }
    }
}