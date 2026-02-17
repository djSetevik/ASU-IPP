namespace ASUIPP.Core.Models
{
    /// <summary>
    /// Настройки приложения, хранятся в таблице Settings (key-value).
    /// </summary>
    public class AppSettings
    {
        public string CurrentTeacherId { get; set; }
        public bool IsHead { get; set; }
        public string DepartmentName { get; set; }      // "Информационные технологии транспорта"
        public string DepartmentShortName { get; set; }  // "ИТТ"
        public string SemesterYear { get; set; }          // "2024-2025"
        public int SemesterNumber { get; set; }           // 1 или 2
        public bool IsFirstRun { get; set; } = true;
        public string ReferenceFilePath { get; set; }     // Путь к исходному Excel
    }
}