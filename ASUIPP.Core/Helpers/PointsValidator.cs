using System;
using System.Linq;

namespace ASUIPP.Core.Helpers
{
    /// <summary>
    /// Парсит строку баллов из Excel-справочника.
    /// Примеры: "7", "3/5/7", "30/50", "-5", "1 балл за 1 практику", "5/10", ""
    /// </summary>
    public static class PointsValidator
    {
        /// <summary>
        /// Извлекает максимально допустимое числовое значение баллов из строки.
        /// Возвращает null, если строка не содержит чисел или пустая.
        /// </summary>
        public static int? ParseMaxPoints(string maxPointsRaw)
        {
            if (string.IsNullOrWhiteSpace(maxPointsRaw))
                return null;

            var cleaned = maxPointsRaw.Trim();

            // Пробуем как одно число (включая отрицательные: "-5")
            if (int.TryParse(cleaned, out var single))
                return single;

            // Пробуем как дробное ("7.0", "5.0")
            if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var dbl))
                return (int)dbl;

            // Пробуем как составное через "/" ("3/5/7", "30/50", "5/10")
            if (cleaned.Contains("/"))
            {
                var parts = cleaned.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var numbers = parts
                    .Select(p => p.Trim())
                    .Where(p => double.TryParse(p, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    .Select(p => (int)double.Parse(p, System.Globalization.CultureInfo.InvariantCulture))
                    .ToList();

                if (numbers.Any())
                    return numbers.Max();
            }

            // Пробуем извлечь первое число из текста ("1 балл за 1 практику" → 1)
            var digits = ExtractFirstNumber(cleaned);
            if (digits.HasValue)
                return digits;

            return null;
        }

        /// <summary>
        /// Проверяет, что введённые баллы не превышают максимум.
        /// Если максимум неизвестен (null) — любое значение допустимо.
        /// </summary>
        public static bool Validate(int points, int? maxPointsNumeric)
        {
            if (!maxPointsNumeric.HasValue)
                return true;

            // Для отрицательных лимитов (штрафы): баллы должны быть >= лимита
            if (maxPointsNumeric.Value < 0)
                return points >= maxPointsNumeric.Value && points <= 0;

            return points >= 0 && points <= maxPointsNumeric.Value;
        }

        /// <summary>
        /// Возвращает строку допустимых значений для отображения.
        /// "макс. 7", "макс. 50 (30/50)", "без ограничений"
        /// </summary>
        public static string GetDisplayString(string maxPointsRaw, int? maxPointsNumeric)
        {
            if (!maxPointsNumeric.HasValue)
                return "без ограничений";

            if (maxPointsNumeric.Value < 0)
                return $"штраф: {maxPointsRaw}";

            var raw = (maxPointsRaw ?? "").Trim();
            if (raw.Contains("/"))
                return $"макс. {maxPointsNumeric} ({raw})";

            return $"макс. {maxPointsNumeric}";
        }

        private static int? ExtractFirstNumber(string text)
        {
            var numStr = "";
            var foundDigit = false;

            foreach (var ch in text)
            {
                if (char.IsDigit(ch) || (ch == '-' && !foundDigit))
                {
                    numStr += ch;
                    foundDigit = true;
                }
                else if (foundDigit)
                {
                    break;
                }
            }

            if (int.TryParse(numStr, out var result))
                return result;

            return null;
        }
    }
}