namespace ASUIPP.Core.Models
{
    /// <summary>
    /// Пункт из справочника работ (загружается из Excel).
    /// Например: Раздел 1, п.14 "Оценка по анкете «Преподаватель глазами студентов»", макс. 7 баллов.
    /// </summary>
    public class WorkItem
    {
        public int SectionId { get; set; }
        public string ItemId { get; set; }          // "1", "2", "4.1", "4.2", "2а", "2б"
        public string Name { get; set; }
        public string MaxPoints { get; set; }        // Исходная строка: "7", "3/5/7", "30/50", "1 балл за 1 практику"
        public int? MaxPointsNumeric { get; set; }   // Максимально допустимое число (null если невозможно определить)
        public int SortOrder { get; set; }

        // Навигационное свойство
        public Section Section { get; set; }

        /// <summary>
        /// Полный идентификатор для отображения: "п.14", "п.4.2"
        /// </summary>
        public string DisplayId => $"п.{ItemId}";
    }
}