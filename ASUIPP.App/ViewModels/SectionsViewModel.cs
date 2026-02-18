using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
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

        public string DisplayName { get; set; }
        public bool IsOverLimit { get; internal set; }
        public int WorksCount { get; internal set; }
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

        public RelayCommand CreateReportCommand { get; }
        public RelayCommand ExportCommand { get; }

        public RelayCommand<SectionDisplayItem> OpenSectionCommand { get; }

        // Событие для навигации в MainWindow
        public event System.Action<int> SectionSelected;

        public SectionsViewModel(DatabaseContext dbContext, string teacherId)
        {
            _dbContext = dbContext;
            _teacherId = teacherId;
            _workRepo = new WorkRepository(dbContext);

            OpenSectionCommand = new RelayCommand<SectionDisplayItem>(item =>
            {
                if (item != null)
                    SectionSelected?.Invoke(item.SectionId);
            });

            LoadData();
        }

        public void LoadData()
        {
            var refRepo = new ReferenceRepository(_dbContext);
            var sections = refRepo.GetAllSections();
            var works = _workRepo.GetByTeacher(_teacherId);

            var teacherRepo = new TeacherRepository(_dbContext);
            var teacher = teacherRepo.GetById(_teacherId);
            TeacherName = teacher?.ShortName ?? teacher?.FullName ?? "";

            Sections.Clear();
            int grandTotal = 0;

            foreach (var sec in sections)
            {
                var sectionWorks = works.Where(w => w.SectionId == sec.SectionId).ToList();
                var rawSum = sectionWorks.Sum(w => w.Points);

                Sections.Add(new SectionDisplayItem
                {
                    SectionId = sec.SectionId,
                    DisplayName = $"{sec.SectionId}. {sec.Name}",
                    TotalPoints = rawSum,
                    WorksCount = sectionWorks.Count
                });

                // Для итого — с лимитом 50 за раздел
                grandTotal += System.Math.Min(rawSum, Core.Helpers.PointsLimits.MaxPerSection);
            }

            // Итого — с лимитом 100
            GrandTotal = System.Math.Min(grandTotal, Core.Helpers.PointsLimits.MaxTotal);

            OnPropertyChanged(nameof(TeacherName));
            OnPropertyChanged(nameof(GrandTotal));
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