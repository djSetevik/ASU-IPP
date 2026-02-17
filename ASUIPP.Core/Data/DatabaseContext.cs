using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace ASUIPP.Core.Data
{
    /// <summary>
    /// Управляет подключением к SQLite базе данных.
    /// Путь по умолчанию: %AppData%\ASUIPP\asuipp.db
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection _connection;

        public static string DefaultDbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ASUIPP", "asuipp.db");

        public DatabaseContext() : this(DefaultDbPath) { }

        public DatabaseContext(string dbPath)
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public IDbConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SQLiteConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
                // Включаем WAL для лучшей производительности и FK для целостности группы бист411
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
                    cmd.ExecuteNonQuery();
                }
            }

            return _connection;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}