namespace ASUIPP.Core.Models
{
    public enum WorkStatus
    {
        Planned = 0,        // Запланирована
        InProgress = 1,     // Выполняется
        Done = 2,           // Выполнена (ожидает подтверждения)
        Confirmed = 3,      // Подтверждена заведующим
        Reported = 4        // Учтена в отчёте
    }
}