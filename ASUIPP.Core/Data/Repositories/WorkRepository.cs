using System;
using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Models;
using Dapper;

namespace ASUIPP.Core.Data.Repositories
{
    public class WorkRepository : IWorkRepository
    {
        private readonly DatabaseContext _context;

        public WorkRepository(DatabaseContext context)
        {
            _context = context;
        }

        // ── PlannedWorks ──────────────────────────────────────

        public PlannedWork GetById(string workId)
        {
            var db = _context.GetConnection();
            var work = db.QueryFirstOrDefault<PlannedWork>(
                "SELECT * FROM PlannedWorks WHERE WorkId = @WorkId",
                new { WorkId = workId });

            if (work != null)
                work.AttachedFiles = GetFilesByWork(workId);

            return work;
        }

        public List<PlannedWork> GetByTeacher(string teacherId)
        {
            var db = _context.GetConnection();
            var works = db.Query<PlannedWork>(@"
                SELECT * FROM PlannedWorks
                WHERE TeacherId = @TeacherId
                ORDER BY SectionId, ItemId",
                new { TeacherId = teacherId }).ToList();

            FillAttachedFiles(works);
            return works;
        }

        public List<PlannedWork> GetByTeacherAndSection(string teacherId, int sectionId)
        {
            var db = _context.GetConnection();
            var works = db.Query<PlannedWork>(@"
                SELECT * FROM PlannedWorks
                WHERE TeacherId = @TeacherId AND SectionId = @SectionId
                ORDER BY ItemId",
                new { TeacherId = teacherId, SectionId = sectionId }).ToList();

            FillAttachedFiles(works);
            return works;
        }

        public List<PlannedWork> GetUpcomingByTeacher(string teacherId, int daysAhead = 30)
        {
            var db = _context.GetConnection();
            var now = DateTime.Today.ToString("o");
            var limit = DateTime.Today.AddDays(daysAhead).ToString("o");

            var works = db.Query<PlannedWork>(@"
                SELECT * FROM PlannedWorks
                WHERE TeacherId = @TeacherId
                  AND DueDate IS NOT NULL
                  AND DueDate <= @Limit
                  AND Status IN (0, 1)
                ORDER BY DueDate",
                new { TeacherId = teacherId, Limit = limit }).ToList();

            FillAttachedFiles(works);
            return works;
        }

        public List<PlannedWork> GetOverdueByTeacher(string teacherId)
        {
            var db = _context.GetConnection();
            var now = DateTime.Today.ToString("o");

            var works = db.Query<PlannedWork>(@"
                SELECT * FROM PlannedWorks
                WHERE TeacherId = @TeacherId
                  AND DueDate IS NOT NULL
                  AND DueDate < @Now
                  AND Status IN (0, 1)
                ORDER BY DueDate",
                new { TeacherId = teacherId, Now = now }).ToList();

            FillAttachedFiles(works);
            return works;
        }

        public void Insert(PlannedWork work)
        {
            var db = _context.GetConnection();
            work.UpdatedAt = DateTime.Now;
            db.Execute(@"
                INSERT INTO PlannedWorks
                (WorkId, TeacherId, SectionId, ItemId, WorkName, Points, DueDate, Status, CreatedAt, UpdatedAt)
                VALUES
                (@WorkId, @TeacherId, @SectionId, @ItemId, @WorkName, @Points, @DueDate, @Status, @CreatedAt, @UpdatedAt)",
                work);
        }

        public void Update(PlannedWork work)
        {
            var db = _context.GetConnection();
            work.UpdatedAt = DateTime.Now;
            db.Execute(@"
                UPDATE PlannedWorks SET
                    SectionId = @SectionId,
                    ItemId = @ItemId,
                    WorkName = @WorkName,
                    Points = @Points,
                    DueDate = @DueDate,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt
                WHERE WorkId = @WorkId", work);
        }

        public void UpdateStatus(string workId, WorkStatus status)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                UPDATE PlannedWorks SET Status = @Status, UpdatedAt = @UpdatedAt
                WHERE WorkId = @WorkId",
                new { WorkId = workId, Status = (int)status, UpdatedAt = DateTime.Now.ToString("o") });
        }

        public void Delete(string workId)
        {
            var db = _context.GetConnection();
            // Файлы удалятся каскадно благодаря ON DELETE CASCADE
            db.Execute("DELETE FROM PlannedWorks WHERE WorkId = @WorkId",
                new { WorkId = workId });
        }

        // ── Суммы баллов ──────────────────────────────────────

        public int GetTotalPointsByTeacher(string teacherId)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<int>(
                "SELECT COALESCE(SUM(Points), 0) FROM PlannedWorks WHERE TeacherId = @TeacherId",
                new { TeacherId = teacherId });
        }

        public int GetSectionPointsByTeacher(string teacherId, int sectionId)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<int>(@"
                SELECT COALESCE(SUM(Points), 0) FROM PlannedWorks
                WHERE TeacherId = @TeacherId AND SectionId = @SectionId",
                new { TeacherId = teacherId, SectionId = sectionId });
        }

        // ── AttachedFiles ──────────────────────────────────────

        public List<AttachedFile> GetFilesByWork(string workId)
        {
            var db = _context.GetConnection();
            return db.Query<AttachedFile>(
                "SELECT * FROM AttachedFiles WHERE WorkId = @WorkId",
                new { WorkId = workId }).ToList();
        }

        public void InsertFile(AttachedFile file)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                INSERT INTO AttachedFiles (FileId, WorkId, FileName, FilePath, FileType)
                VALUES (@FileId, @WorkId, @FileName, @FilePath, @FileType)", file);
        }

        public void DeleteFile(string fileId)
        {
            var db = _context.GetConnection();
            db.Execute("DELETE FROM AttachedFiles WHERE FileId = @FileId",
                new { FileId = fileId });
        }

        public void DeleteFilesByWork(string workId)
        {
            var db = _context.GetConnection();
            db.Execute("DELETE FROM AttachedFiles WHERE WorkId = @WorkId",
                new { WorkId = workId });
        }

        public List<PlannedWork> GetByTeacherAndPeriod(string teacherId, int periodId)
        {
            var db = _context.GetConnection();
            return db.Query<PlannedWork>(
                "SELECT * FROM PlannedWorks WHERE TeacherId = @TeacherId AND (PeriodId = @PeriodId OR PeriodId = 0)",
                new { TeacherId = teacherId, PeriodId = periodId }).ToList();
        }

        public List<PlannedWork> GetByTeacherSectionAndPeriod(string teacherId, int sectionId, int periodId)
        {
            var db = _context.GetConnection();
            return db.Query<PlannedWork>(
                "SELECT * FROM PlannedWorks WHERE TeacherId = @TeacherId AND SectionId = @SectionId AND (PeriodId = @PeriodId OR PeriodId = 0)",
                new { TeacherId = teacherId, SectionId = sectionId, PeriodId = periodId }).ToList();
        }

        // ── Вспомогательные ────────────────────────────────────
        //уээээээээуэуэуээуэуэуэуэуэуэуэ
        private void FillAttachedFiles(List<PlannedWork> works)
        {
            if (!works.Any()) return;

            var db = _context.GetConnection();
            var workIds = works.Select(w => w.WorkId).ToList();

            var allFiles = db.Query<AttachedFile>(
                "SELECT * FROM AttachedFiles WHERE WorkId IN @WorkIds",
                new { WorkIds = workIds }).ToList();

            var grouped = allFiles.GroupBy(f => f.WorkId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var work in works)
            {
                if (grouped.TryGetValue(work.WorkId, out var files))
                    work.AttachedFiles = files;
            }
        }
    }
}