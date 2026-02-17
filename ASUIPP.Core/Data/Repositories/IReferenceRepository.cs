using System.Collections.Generic;
using ASUIPP.Core.Models;

namespace ASUIPP.Core.Data.Repositories
{
    public interface IReferenceRepository
    {
        // Разделы
        List<Section> GetAllSections();
        void InsertSection(Section section);
        void ClearAll(); // Очистка при повторном импорте справочника

        // Пункты работ
        List<WorkItem> GetWorkItemsBySection(int sectionId);
        List<WorkItem> GetAllWorkItems();
        WorkItem GetWorkItem(int sectionId, string itemId);
        void InsertWorkItem(WorkItem item);
    }
}