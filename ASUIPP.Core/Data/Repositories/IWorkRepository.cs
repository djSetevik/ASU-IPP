using System.Collections.Generic;
using ASUIPP.Core.Models;

namespace ASUIPP.Core.Data.Repositories
{
    public interface IWorkRepository
    {
        // PlannedWorks
        PlannedWork GetById(string workId);
        List<PlannedWork> GetByTeacher(string teacherId);
        List<PlannedWork> GetByTeacherAndSection(string teacherId, int sectionId);
        List<PlannedWork> GetUpcomingByTeacher(string teacherId, int daysAhead = 30);
        List<PlannedWork> GetOverdueByTeacher(string teacherId);
        void Insert(PlannedWork work);
        void Update(PlannedWork work);
        void UpdateStatus(string workId, WorkStatus status);
        void Delete(string workId);

        // Суммы баллов
        int GetTotalPointsByTeacher(string teacherId);
        int GetSectionPointsByTeacher(string teacherId, int sectionId);

        // AttachedFiles
        List<AttachedFile> GetFilesByWork(string workId);
        void InsertFile(AttachedFile file);
        void DeleteFile(string fileId);
        void DeleteFilesByWork(string workId);
    }
}