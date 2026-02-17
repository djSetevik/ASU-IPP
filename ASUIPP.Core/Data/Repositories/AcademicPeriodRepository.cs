using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Models;
using Dapper;

namespace ASUIPP.Core.Data.Repositories
{
    public class AcademicPeriodRepository
    {
        private readonly DatabaseContext _context;

        public AcademicPeriodRepository(DatabaseContext context)
        {
            _context = context;
        }

        public List<AcademicPeriod> GetAll()
        {
            var db = _context.GetConnection();
            if (db == null) return new List<AcademicPeriod>();

            try
            {
                return db.Query<AcademicPeriod>(
                    "SELECT * FROM AcademicPeriods ORDER BY YearStart DESC, Semester DESC"
                ).ToList();
            }
            catch
            {
                // Таблица ещё не существует — создаём
                db.Execute(@"
            CREATE TABLE IF NOT EXISTS AcademicPeriods (
                PeriodId INTEGER PRIMARY KEY AUTOINCREMENT,
                YearStart INTEGER NOT NULL,
                Semester INTEGER NOT NULL,
                UNIQUE(YearStart, Semester)
            )");
                return new List<AcademicPeriod>();
            }
        }

        public AcademicPeriod GetById(int periodId)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<AcademicPeriod>(
                "SELECT * FROM AcademicPeriods WHERE PeriodId = @PeriodId",
                new { PeriodId = periodId });
        }

        public AcademicPeriod GetOrCreate(int yearStart, int semester)
        {
            var db = _context.GetConnection();

            // Гарантируем что таблица есть
            db.Execute(@"
        CREATE TABLE IF NOT EXISTS AcademicPeriods (
            PeriodId INTEGER PRIMARY KEY AUTOINCREMENT,
            YearStart INTEGER NOT NULL,
            Semester INTEGER NOT NULL,
            UNIQUE(YearStart, Semester)
        )");

            var existing = db.QueryFirstOrDefault<AcademicPeriod>(
                "SELECT * FROM AcademicPeriods WHERE YearStart = @YearStart AND Semester = @Semester",
                new { YearStart = yearStart, Semester = semester });

            if (existing != null) return existing;

            db.Execute(
                "INSERT INTO AcademicPeriods (YearStart, Semester) VALUES (@YearStart, @Semester)",
                new { YearStart = yearStart, Semester = semester });

            return db.QueryFirstOrDefault<AcademicPeriod>(
                "SELECT * FROM AcademicPeriods WHERE YearStart = @YearStart AND Semester = @Semester",
                new { YearStart = yearStart, Semester = semester });
        }
    }
}