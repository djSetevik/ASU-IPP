namespace ASUIPP.Core.Models
{
    public class AcademicPeriod
    {
        public int PeriodId { get; set; }
        public int YearStart { get; set; } // 2024 для "2024-2025"
        public int Semester { get; set; }  // 1 или 2

        public string DisplayName => $"{YearStart}-{YearStart + 1}, {Semester} семестр";
    }
}