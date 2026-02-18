using ASUIPP.Core.Models;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace ASUIPP.Core.Data.Repositories
{
    public class WorkItemRepository
    {
        private readonly DatabaseContext _context;

        public WorkItemRepository(DatabaseContext context)
        {
            _context = context;
        }

        public List<WorkItem> GetBySection(int sectionId)
        {
            var db = _context.GetConnection();
            return db.Query<WorkItem>(
                "SELECT * FROM WorkItems WHERE SectionId = @SectionId ORDER BY SortOrder",
                new { SectionId = sectionId }).ToList();
        }

        public void InsertOrUpdate(WorkItem item)
        {
            var db = _context.GetConnection();
            db.Execute(@"INSERT OR REPLACE INTO WorkItems 
                         (SectionId, ItemId, Name, MaxPoints, MaxPointsNumeric, SortOrder)
                         VALUES (@SectionId, @ItemId, @Name, @MaxPoints, @MaxPointsNumeric, @SortOrder)", item);
        }
    }
}