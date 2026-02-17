using System;
using System.Collections.Generic;

namespace ASUIPP.Core.Models
{
    public class PlannedWork
    {
        public string WorkId { get; set; }        // GUID
        public string TeacherId { get; set; }     // FK → Teacher
        public int SectionId { get; set; }         // FK → Section (1-5)
        public string ItemId { get; set; }         // FK → WorkItem ("14", "4.2")
        public string WorkName { get; set; }       // Пользовательское название работы
        public int Points { get; set; }
        public DateTime? DueDate { get; set; }
        public WorkStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Навигационные свойства
        public WorkItem WorkItem { get; set; }
        public List<AttachedFile> AttachedFiles { get; set; } = new List<AttachedFile>();

        public PlannedWork()
        {
            WorkId = Guid.NewGuid().ToString();
            Status = WorkStatus.Planned;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Дней до дедлайна. Отрицательное значение = просрочено.
        /// </summary>
        public int? DaysUntilDue => DueDate.HasValue
            ? (int)(DueDate.Value.Date - DateTime.Today).TotalDays
            : (int?)null;
    }
}