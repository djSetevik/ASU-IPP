using ASUIPP.Core.Models;
using Dapper;

namespace ASUIPP.Core.Data.Repositories
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly DatabaseContext _context;

        public SettingsRepository(DatabaseContext context)
        {
            _context = context;
        }

        public string Get(string key)
        {
            var db = _context.GetConnection();
            return db.QueryFirstOrDefault<string>(
                "SELECT Value FROM Settings WHERE Key = @Key", new { Key = key });
        }

        public void Set(string key, string value)
        {
            var db = _context.GetConnection();
            db.Execute(@"
                INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)
                ON CONFLICT(Key) DO UPDATE SET Value = @Value",
                new { Key = key, Value = value });
        }

        public AppSettings GetAppSettings()
        {
            return new AppSettings
            {
                CurrentTeacherId = Get("CurrentTeacherId"),
                IsHead = Get("IsHead") == "1",
                DepartmentName = Get("DepartmentName"),
                DepartmentShortName = Get("DepartmentShortName"),
                SemesterYear = Get("SemesterYear"),
                SemesterNumber = int.TryParse(Get("SemesterNumber"), out var sn) ? sn : 0,
                IsFirstRun = Get("IsFirstRun") != "0",
                ReferenceFilePath = Get("ReferenceFilePath")
            };
        }

        public void SaveAppSettings(AppSettings settings)
        {
            Set("CurrentTeacherId", settings.CurrentTeacherId ?? "");
            Set("IsHead", settings.IsHead ? "1" : "0");
            Set("DepartmentName", settings.DepartmentName ?? "");
            Set("DepartmentShortName", settings.DepartmentShortName ?? "");
            Set("SemesterYear", settings.SemesterYear ?? "");
            Set("SemesterNumber", settings.SemesterNumber.ToString());
            Set("IsFirstRun", settings.IsFirstRun ? "1" : "0");
            Set("ReferenceFilePath", settings.ReferenceFilePath ?? "");
        }
    }
}