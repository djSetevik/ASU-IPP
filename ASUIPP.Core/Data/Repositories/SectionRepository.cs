using ASUIPP.Core.Models;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace ASUIPP.Core.Data.Repositories
{
    public class SectionRepository
    {
        private readonly DatabaseContext _context;

        public SectionRepository(DatabaseContext context)
        {
            _context = context;
        }

        public List<Section> GetAll()
        {
            var db = _context.GetConnection();
            return db.Query<Section>(
                "SELECT * FROM Sections ORDER BY SortOrder").ToList();
        }

        public void InsertOrUpdate(Section section)
        {
            var db = _context.GetConnection();
            db.Execute(@"INSERT OR REPLACE INTO Sections (SectionId, Name, SortOrder)
                         VALUES (@SectionId, @Name, @SortOrder)", section);
        }
    }
}