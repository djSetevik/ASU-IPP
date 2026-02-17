using System;
using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Models;

namespace ASUIPP.Core.Services
{
    public class ReminderInfo
    {
        public string WorkId { get; set; }
        public string WorkName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DaysUntilDue { get; set; }
        public WorkStatus Status { get; set; }
        public bool IsOverdue => DaysUntilDue.HasValue && DaysUntilDue.Value < 0;
    }

    public class ReminderService
    {
        private readonly WorkRepository _workRepo;
        private readonly ReferenceRepository _refRepo;

        public ReminderService(DatabaseContext dbContext)
        {
            _workRepo = new WorkRepository(dbContext);
            _refRepo = new ReferenceRepository(dbContext);
        }

        /// <summary>
        /// Возвращает список работ, требующих внимания:
        /// просроченные + ближайшие дедлайны (Planned/InProgress).
        /// </summary>
        public List<ReminderInfo> GetReminders(string teacherId, int daysAhead = 60)
        {
            var sections = _refRepo.GetAllSections()
                .ToDictionary(s => s.SectionId, s => s.Name);

            var overdue = _workRepo.GetOverdueByTeacher(teacherId);
            var upcoming = _workRepo.GetUpcomingByTeacher(teacherId, daysAhead);

            var all = overdue.Concat(upcoming)
                .GroupBy(w => w.WorkId)
                .Select(g => g.First())
                .OrderBy(w => w.DueDate)
                .Select(w => new ReminderInfo
                {
                    WorkId = w.WorkId,
                    WorkName = w.WorkName,
                    SectionId = w.SectionId,
                    SectionName = sections.ContainsKey(w.SectionId) ? sections[w.SectionId] : "",
                    DueDate = w.DueDate,
                    DaysUntilDue = w.DaysUntilDue,
                    Status = w.Status
                })
                .ToList();

            return all;
        }

        /// <summary>
        /// Количество просроченных работ (для бейджа в трее).
        /// </summary>
        public int GetOverdueCount(string teacherId)
        {
            return _workRepo.GetOverdueByTeacher(teacherId).Count;
        }
    }
}