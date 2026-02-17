using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Models;
using Dapper;

namespace ASUIPP.Core.Data.Repositories
{
    public class ReferenceRepository : IReferenceRepository
    {
        private readonly DatabaseContext _context;

        public ReferenceRepository(DatabaseContext context)
        {
            _context = context;
        }

        public List<Section> GetAllSections()
        {
            var db = _context.GetConnection();
            return db.Query<Section>(
                "SELECT SectionId, Name, SortOrder FROM Sections ORDER BY SortOrder"
            ).ToList();
        }

        public void InsertSection(Section section)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                INSERT OR REPLACE INTO Sections (SectionId, Name, SortOrder)
                VALUES (@SectionId, @Name, @SortOrder)", section);
        }

        public List<WorkItem> GetWorkItemsBySection(int sectionId)
        {
            var db = _context.GetConnection();
            return db.Query<WorkItem>(@"
                SELECT SectionId, ItemId, Name, MaxPoints, MaxPointsNumeric, SortOrder
                FROM WorkItems WHERE SectionId = @SectionId
                ORDER BY SortOrder",
                new { SectionId = sectionId }
            ).ToList();
        }

        public List<WorkItem> GetAllWorkItems()
        {
            var db = _context.GetConnection();
            return db.Query<WorkItem>(@"
                SELECT SectionId, ItemId, Name, MaxPoints, MaxPointsNumeric, SortOrder
                FROM WorkItems ORDER BY SectionId, SortOrder"
            ).ToList();
        }

        public WorkItem GetWorkItem(int sectionId, string itemId)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<WorkItem>(@"
                SELECT SectionId, ItemId, Name, MaxPoints, MaxPointsNumeric, SortOrder
                FROM WorkItems WHERE SectionId = @SectionId AND ItemId = @ItemId",
                new { SectionId = sectionId, ItemId = itemId });
        }

        public void InsertWorkItem(WorkItem item)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                INSERT OR REPLACE INTO WorkItems
                (SectionId, ItemId, Name, MaxPoints, MaxPointsNumeric, SortOrder)
                VALUES (@SectionId, @ItemId, @Name, @MaxPoints, @MaxPointsNumeric, @SortOrder)", item);
        }

        public void ClearAll()
        {
            var db = _context.GetConnection();
            db.Execute("DELETE FROM WorkItems;");
            db.Execute("DELETE FROM Sections;");
        }
    }
}