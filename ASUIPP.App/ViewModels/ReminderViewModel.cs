using System.Collections.ObjectModel;
using System.Linq;
using ASUIPP.App.Helpers;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;

namespace ASUIPP.App.ViewModels
{
    public class ReminderItem : ViewModelBase
    {
        public string WorkId { get; set; }
        public string WorkName { get; set; }
        public int SectionId { get; set; }
        public int? DaysUntilDue { get; set; }

        public string DaysText
        {
            get
            {
                if (!DaysUntilDue.HasValue) return "";
                var d = DaysUntilDue.Value;
                if (d < 0) return $"ПРОСРОЧЕНО на {System.Math.Abs(d)} дн.";
                if (d == 0) return "Сегодня!";
                if (d == 1) return "Завтра";
                return $"осталось {d} дн.";
            }
        }

        public bool IsOverdue => DaysUntilDue.HasValue && DaysUntilDue.Value < 0;
        public bool IsUrgent => DaysUntilDue.HasValue && DaysUntilDue.Value >= 0 && DaysUntilDue.Value <= 3;
    }

    public class ReminderViewModel : ViewModelBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string _teacherId;

        public ObservableCollection<ReminderItem> Items { get; }
            = new ObservableCollection<ReminderItem>();

        private string _welcomeName;
        public string WelcomeName
        {
            get => _welcomeName;
            set => SetProperty(ref _welcomeName, value);
        }

        public event System.Action<string, int> OpenWorkRequested;

        public RelayCommand<ReminderItem> OpenWorkCommand { get; }

        public ReminderViewModel(DatabaseContext dbContext, string teacherId)
        {
            _dbContext = dbContext;
            _teacherId = teacherId;

            OpenWorkCommand = new RelayCommand<ReminderItem>(item =>
            {
                if (item != null)
                    OpenWorkRequested?.Invoke(item.WorkId, item.SectionId);
            });

            var teacherRepo = new TeacherRepository(dbContext);
            var teacher = teacherRepo.GetById(teacherId);
            WelcomeName = teacher?.ShortName ?? "Пользователь";

            LoadData();
        }

        public void LoadData()
        {
            var workRepo = new WorkRepository(_dbContext);
            var upcoming = workRepo.GetUpcomingByTeacher(_teacherId, 365);
            var overdue = workRepo.GetOverdueByTeacher(_teacherId);

            var all = overdue.Concat(upcoming)
                .Where(w => w.Status != WorkStatus.Confirmed) // не показываем подтверждённые
                .GroupBy(w => w.WorkId)
                .Select(g => g.First())
                .OrderBy(w => w.DueDate)
                .ToList();

            Items.Clear();
            foreach (var w in all)
            {
                Items.Add(new ReminderItem
                {
                    WorkId = w.WorkId,
                    WorkName = w.WorkName,
                    SectionId = w.SectionId,
                    DaysUntilDue = w.DaysUntilDue
                });
            }
        }
    }
}