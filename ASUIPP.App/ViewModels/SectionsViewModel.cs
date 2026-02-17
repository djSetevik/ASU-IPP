using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASUIPP.App.ViewModels
{
    public class SectionDisplayItem : ViewModelBase
    {
        public int SectionId { get; set; }
        public string Name { get; set; }

        private int _totalPoints;
        public int TotalPoints
        {
            get => _totalPoints;
            set => SetProperty(ref _totalPoints, value);
        }

        public string DisplayName => $"{SectionId}. {Name}";
    }

    public class SectionsViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string _teacherId;
        private readonly WorkRepository _workRepo;
        private readonly ReferenceRepository _refRepo;
        private readonly TeacherRepository _teacherRepo;

        public ObservableCollection<SectionDisplayItem> Sections { get; }
            = new ObservableCollection<SectionDisplayItem>();

        private int _grandTotal;
        public int GrandTotal
        {
            get => _grandTotal;
            set => SetProperty(ref _grandTotal, value);
        }

        private string _teacherName;
        public string TeacherName
        {
            get => _teacherName;
            set => SetProperty(ref _teacherName, value);
        }

        private SectionDisplayItem _selectedSection;
        public SectionDisplayItem SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }

        public RelayCommand OpenSectionCommand { get; }
        public RelayCommand CreateReportCommand { get; }
        public RelayCommand ExportCommand { get; }

        // Событие для навигации в MainWindow
        public event System.Action<int> SectionSelected;

        public SectionsViewModel(DatabaseContext dbContext, string teacherId)
        {
            _dbContext = dbContext;
            _teacherId = teacherId;
            _workRepo = new WorkRepository(dbContext);
            _refRepo = new ReferenceRepository(dbContext);
            _teacherRepo = new TeacherRepository(dbContext);

            OpenSectionCommand = new RelayCommand(o =>
            {
                if (o is SectionDisplayItem item)
                    SectionSelected?.Invoke(item.SectionId);
                else if (SelectedSection != null)
                    SectionSelected?.Invoke(SelectedSection.SectionId);
            });

            CreateReportCommand = new RelayCommand(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Сохранить индивидуальный отчёт",
                        Filter = "Excel (*.xlsx)|*.xlsx",
                        FileName = $"Отчёт_{DateTime.Now:yyyy-MM-dd}.xlsx"
                    };
                    if (dialog.ShowDialog() != true) return;

                    var reportService = new Core.Services.ReportService(_dbContext);
                    var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
                    var filePath = reportService.GeneratePersonalReport(_teacherId, dir);

                    // Переименуем если пользователь выбрал другое имя
                    if (filePath != dialog.FileName && System.IO.File.Exists(filePath))
                    {
                        if (System.IO.File.Exists(dialog.FileName))
                            System.IO.File.Delete(dialog.FileName);
                        System.IO.File.Move(filePath, dialog.FileName);
                        filePath = dialog.FileName;
                    }

                    var result = System.Windows.MessageBox.Show(
                        $"Отчёт сохранён:\n{filePath}\n\nОткрыть?",
                        "АСУИПП", System.Windows.MessageBoxButton.YesNo);
                    if (result == System.Windows.MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "АСУИПП");
                }
            });

            ExportCommand = new RelayCommand(() =>
            {
                try
                {
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Сохранить архив данных",
                        Filter = "ZIP-архив (*.zip)|*.zip",
                        FileName = $"АСУИПП_{DateTime.Now:yyyy-MM-dd}.zip"
                    };
                    if (saveDialog.ShowDialog() != true) return;

                    var dir = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                    var workRepo = new Core.Data.Repositories.WorkRepository(_dbContext);
                    var teacherRepo = new Core.Data.Repositories.TeacherRepository(_dbContext);
                    var settingsRepo = new Core.Data.Repositories.SettingsRepository(_dbContext);
                    var exportService = new Core.Services.ExportService(workRepo, teacherRepo, settingsRepo);
                    var filePath = exportService.Export(_teacherId, dir);

                    // Переименуем если нужно
                    if (filePath != saveDialog.FileName && System.IO.File.Exists(filePath))
                    {
                        if (System.IO.File.Exists(saveDialog.FileName))
                            System.IO.File.Delete(saveDialog.FileName);
                        System.IO.File.Move(filePath, saveDialog.FileName);
                        filePath = saveDialog.FileName;
                    }

                    System.Windows.MessageBox.Show($"Архив сохранён:\n{filePath}", "АСУИПП");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "АСУИПП");
                }
            });

            LoadData();
        }

        public void LoadData()
        {
            var teacher = _teacherRepo.GetById(_teacherId);
            TeacherName = teacher?.FullName ?? "Неизвестный";

            var sections = _refRepo.GetAllSections();

            Sections.Clear();
            foreach (var sec in sections)
            {
                var points = _workRepo.GetSectionPointsByTeacher(_teacherId, sec.SectionId);
                Sections.Add(new SectionDisplayItem
                {
                    SectionId = sec.SectionId,
                    Name = sec.Name,
                    TotalPoints = points
                });
            }

            GrandTotal = Sections.Sum(s => s.TotalPoints);
        }

        public void Refresh()
        {
            foreach (var sec in Sections)
            {
                sec.TotalPoints = _workRepo.GetSectionPointsByTeacher(_teacherId, sec.SectionId);
            }
            GrandTotal = Sections.Sum(s => s.TotalPoints);
        }
    }
}