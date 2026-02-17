using System.Collections.Generic;
using System.Linq;
using ASUIPP.Core.Models;

namespace ASUIPP.Core.Helpers
{
    public static class PointsLimits
    {
        public const int MaxPerSection = 50;
        public const int MaxTotal = 100;

        /// <summary>
        /// Проверяет можно ли добавить/изменить баллы.
        /// Возвращает null если ОК, иначе текст ошибки.
        /// </summary>
        public static string Validate(int newPoints, int sectionId,
            string workIdToExclude, List<PlannedWork> allWorks)
        {
            // Сумма по разделу (исключая текущую работу при редактировании)
            var sectionSum = allWorks
                .Where(w => w.SectionId == sectionId && w.WorkId != workIdToExclude)
                .Sum(w => w.Points);

            if (sectionSum + newPoints > MaxPerSection)
            {
                var available = MaxPerSection - sectionSum;
                return $"Превышен лимит раздела ({MaxPerSection} баллов).\n" +
                       $"Текущая сумма: {sectionSum}, доступно: {available}";
            }

            // Общая сумма
            var totalSum = allWorks
                .Where(w => w.WorkId != workIdToExclude)
                .Sum(w => w.Points);

            if (totalSum + newPoints > MaxTotal)
            {
                var available = MaxTotal - totalSum;
                return $"Превышен общий лимит ({MaxTotal} баллов).\n" +
                       $"Текущая сумма: {totalSum}, доступно: {available}";
            }

            return null;
        }

        /// <summary>
        /// Эффективная сумма по разделу (не больше MaxPerSection).
        /// </summary>
        public static int EffectiveSectionSum(List<PlannedWork> works, int sectionId)
        {
            var raw = works.Where(w => w.SectionId == sectionId).Sum(w => w.Points);
            return System.Math.Min(raw, MaxPerSection);
        }

        /// <summary>
        /// Эффективная общая сумма (не больше MaxTotal).
        /// </summary>
        public static int EffectiveTotal(List<PlannedWork> works, List<int> sectionIds)
        {
            var sum = sectionIds.Sum(sid => EffectiveSectionSum(works, sid));
            return System.Math.Min(sum, MaxTotal);
        }
    }
}