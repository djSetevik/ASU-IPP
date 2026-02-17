using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Models;
using Dapper;

namespace ASUIPP.Core.Data.Repositories
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly DatabaseContext _context;

        public TeacherRepository(DatabaseContext context)
        {
            _context = context;
        }

        public Teacher GetById(string teacherId)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<Teacher>(
                "SELECT * FROM Teachers WHERE TeacherId = @TeacherId",
                new { TeacherId = teacherId });
        }

        public Teacher GetByFullName(string fullName)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<Teacher>(
                "SELECT * FROM Teachers WHERE FullName = @FullName",
                new { FullName = fullName });
        }

        public List<Teacher> GetAll()
        {
            var db = _context.GetConnection();
            return db.Query<Teacher>(
                "SELECT * FROM Teachers ORDER BY ShortName"
            ).ToList();
        }

        public void Insert(Teacher teacher)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                INSERT OR IGNORE INTO Teachers (TeacherId, FullName, ShortName, IsHead, CreatedAt)
                VALUES (@TeacherId, @FullName, @ShortName, @IsHead, @CreatedAt)", teacher);
        }

        public void Update(Teacher teacher)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                UPDATE Teachers SET FullName = @FullName, ShortName = @ShortName,
                IsHead = @IsHead WHERE TeacherId = @TeacherId", teacher);
        }

        public void Delete(string teacherId)
        {
            var db = _context.GetConnection();
            db.Execute("DELETE FROM Teachers WHERE TeacherId = @TeacherId",
                new { TeacherId = teacherId });
        }

        public bool ExistsByFullName(string fullName)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<int>(
                "SELECT COUNT(*) FROM Teachers WHERE FullName = @FullName",
                new { FullName = fullName }) > 0;
        }
    }
}