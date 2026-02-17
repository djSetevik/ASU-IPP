using Dapper;

namespace ASUIPP.Core.Data
{
    /// <summary>
    /// Создаёт таблицы при первом запуске и выполняет миграции при обновлениях.
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly DatabaseContext _context;

        public DatabaseInitializer(DatabaseContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            var db = _context.GetConnection();

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
            ");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS Sections (
                    SectionId INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0
                );
            ");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS WorkItems (
                    SectionId INTEGER NOT NULL,
                    ItemId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    MaxPoints TEXT NOT NULL DEFAULT '0',
                    MaxPointsNumeric INTEGER,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (SectionId, ItemId),
                    FOREIGN KEY (SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE
                );
            ");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS Teachers (
                    TeacherId TEXT PRIMARY KEY,
                    FullName TEXT NOT NULL,
                    ShortName TEXT NOT NULL,
                    IsHead INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                );
            ");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS PlannedWorks (
                    WorkId TEXT PRIMARY KEY,
                    TeacherId TEXT NOT NULL,
                    SectionId INTEGER NOT NULL,
                    ItemId TEXT NOT NULL,
                    WorkName TEXT NOT NULL,
                    Points INTEGER NOT NULL DEFAULT 0,
                    DueDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (TeacherId) REFERENCES Teachers(TeacherId) ON DELETE CASCADE,
                    FOREIGN KEY (SectionId, ItemId) REFERENCES WorkItems(SectionId, ItemId)
                );
            ");

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS AttachedFiles (
                    FileId TEXT PRIMARY KEY,
                    WorkId TEXT NOT NULL,
                    FileName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    FileType TEXT,
                    FOREIGN KEY (WorkId) REFERENCES PlannedWorks(WorkId) ON DELETE CASCADE
                );
            ");

            // Индексы для частых запросов
            db.Execute("CREATE INDEX IF NOT EXISTS IX_PlannedWorks_TeacherId ON PlannedWorks(TeacherId);");
            db.Execute("CREATE INDEX IF NOT EXISTS IX_PlannedWorks_SectionId ON PlannedWorks(SectionId);");
            db.Execute("CREATE INDEX IF NOT EXISTS IX_PlannedWorks_DueDate ON PlannedWorks(DueDate);");
            db.Execute("CREATE INDEX IF NOT EXISTS IX_AttachedFiles_WorkId ON AttachedFiles(WorkId);");

            // Версия схемы для будущих миграций
            EnsureSetting("SchemaVersion", "1");
        }

        private void EnsureSetting(string key, string value)
        {
            var db = _context.GetConnection();
            var existing = db.QueryFirstOrDefault<string>(
                "SELECT Value FROM Settings WHERE Key = @Key", new { Key = key });

            if (existing == null)
            {
                db.Execute("INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)",
                    new { Key = key, Value = value });
            }
        }
    }
}