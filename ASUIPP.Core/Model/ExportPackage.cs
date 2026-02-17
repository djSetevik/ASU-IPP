using System;
using System.Collections.Generic;

namespace ASUIPP.Core.Models
{
    /// <summary>
    /// Модель для сериализации в data.json при экспорте/импорте ZIP-архива.
    /// </summary>
    public class ExportPackage
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; }
        public ExportTeacher Teacher { get; set; }
        public ExportSemester Semester { get; set; }
        public List<ExportWork> Works { get; set; } = new List<ExportWork>();
    }

    public class ExportTeacher
    {
        public string TeacherId { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
    }

    public class ExportSemester
    {
        public string Year { get; set; }       // "2024-2025"
        public int Number { get; set; }         // 1 или 2
    }

    public class ExportWork
    {
        public string WorkId { get; set; }
        public int SectionId { get; set; }
        public string ItemId { get; set; }
        public string WorkName { get; set; }
        public int Points { get; set; }
        public string DueDate { get; set; }     // ISO 8601
        public string Status { get; set; }       // Имя enum
        public List<ExportFile> AttachedFiles { get; set; } = new List<ExportFile>();
    }

    public class ExportFile
    {
        public string FileName { get; set; }
        public string RelativePath { get; set; } // Путь внутри архива: "files/{WorkId}/scan1.jpg"
    }
}