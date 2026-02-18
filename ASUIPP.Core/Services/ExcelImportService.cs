using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASUIPP.Core.Services
{
    public class ExcelImportService
    {
        private readonly DatabaseContext _context;

        public ExcelImportService(DatabaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Результат импорта.
        /// </summary>
        public class ImportResult
        {
            public int SectionsCount { get; set; }
            public int WorkItemsCount { get; set; }
            public int TeachersCount { get; set; }
            public int ScoresCount { get; set; }
            public string Semester { get; set; }
            public string Year { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        /// <summary>
        /// Полный импорт: разделы, пункты работ, преподаватели и их баллы.
        /// </summary>
        public ImportResult ImportAll(string excelPath)
        {
            var result = new ImportResult();

            var sectionRepo = new SectionRepository(_context);
            var workItemRepo = new WorkItemRepository(_context);
            var teacherRepo = new TeacherRepository(_context);
            var workRepo = new WorkRepository(_context);

            using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
            {
                var workbook = new HSSFWorkbook(fs);
                var sheet = workbook.GetSheetAt(0);

                // ── 1. Читаем заголовок (семестр, год) ──
                var headerRow = sheet.GetRow(0);
                if (headerRow != null)
                {
                    var semCell = headerRow.GetCell(4);
                    result.Semester = GetCellString(semCell);
                    var yearCell = headerRow.GetCell(8);
                    result.Year = GetCellString(yearCell);
                }

                // ── 2. Читаем ФИО преподавателей из строки 4 ──
                var teacherRow = sheet.GetRow(4);
                var teacherColumns = new Dictionary<int, Teacher>(); // colIndex → Teacher

                if (teacherRow != null)
                {
                    for (int col = 3; col < teacherRow.LastCellNum; col++)
                    {
                        var cell = teacherRow.GetCell(col);
                        if (cell == null) continue;
                        var name = GetCellString(cell);
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        // Создаём или находим преподавателя
                        var existing = teacherRepo.GetByFullName(name);
                        if (existing == null)
                        {
                            // Имя в формате "Хабаров В.И." — это уже ShortName
                            // FullName = ShortName в данном случае
                            existing = new Teacher
                            {
                                TeacherId = Guid.NewGuid().ToString(),
                                FullName = name,
                                ShortName = name,
                                IsHead = false,
                                CreatedAt = DateTime.Now
                            };
                            teacherRepo.Insert(existing);
                            result.TeachersCount++;
                        }
                        teacherColumns[col] = existing;
                    }
                }

                // ── 3. Парсим разделы и пункты (существующая логика) ──
                int currentSectionId = 0;
                int sortOrder = 0;
                int itemSortOrder = 0;

                // Двухпроходный алгоритм
                // Проход 1: собираем все разделы и пункты
                var sections = new List<Section>();
                var workItems = new List<WorkItem>();
                var itemRows = new List<ItemRowData>(); // для привязки баллов

                for (int rowIdx = 5; rowIdx <= sheet.LastRowNum; rowIdx++)
                {
                    var row = sheet.GetRow(rowIdx);
                    if (row == null) continue;

                    var cellA = GetCellString(row.GetCell(0));
                    var cellB = GetCellString(row.GetCell(1));

                    // Проверяем заголовок раздела
                    int detectedSection = DetectSectionHeader(cellA, cellB);
                    if (detectedSection > 0)
                    {
                        currentSectionId = detectedSection;
                        var sectionName = ExtractSectionName(cellA, cellB);

                        if (!sections.Any(s => s.SectionId == currentSectionId))
                        {
                            sections.Add(new Section
                            {
                                SectionId = currentSectionId,
                                Name = sectionName,
                                SortOrder = ++sortOrder
                            });
                        }
                        itemSortOrder = 0;
                        continue;
                    }

                    if (currentSectionId == 0) continue;

                    // Строка "Итого" — конец
                    if (cellA.ToLower().StartsWith("итого") || cellB.ToLower().StartsWith("итого"))
                        break;

                    // Определяем ItemId
                    string itemId = null;
                    string itemName = cellB;

                    if (!string.IsNullOrEmpty(cellA))
                    {
                        // cellA может быть числом (1, 2, 3) или текстом (4.1, 4.2)
                        var cleaned = cellA.Replace("\xa0", "").Trim();
                        if (Regex.IsMatch(cleaned, @"^[\d.]+$"))
                        {
                            itemId = cleaned;
                        }
                    }

                    // Подпункты: cellA пустой, cellB начинается с "-" или "а)" и т.п.
                    if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(cellB))
                    {
                        var trimmed = cellB.TrimStart(' ', '-', '\xa0');
                        if (trimmed != cellB || cellB.StartsWith("     "))
                        {
                            // Это подпункт — генерируем ID
                            // Берём последний основной пункт и добавляем подбукву
                            var lastItem = workItems.LastOrDefault(w => w.SectionId == currentSectionId);
                            if (lastItem != null)
                            {
                                var subCount = workItems.Count(w =>
                                    w.SectionId == currentSectionId &&
                                    w.ItemId.StartsWith(lastItem.ItemId.Split('.')[0]) &&
                                    w.ItemId != lastItem.ItemId);

                                var subLetters = "абвгдежзик";
                                var subLetter = subCount < subLetters.Length
                                    ? subLetters[subCount].ToString()
                                    : (subCount + 1).ToString();

                                var baseId = lastItem.ItemId.Contains(".")
                                    ? lastItem.ItemId.Split('.')[0]
                                    : lastItem.ItemId;
                                itemId = $"{baseId}{subLetter}";
                                itemName = cellB.Trim();
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(itemId)) continue;
                    if (string.IsNullOrEmpty(itemName)) continue;

                    // Баллы (колонка 2)
                    var pointsStr = CleanPoints(GetCellString(row.GetCell(2)));

                    // Проверяем что это не контейнер (пункт без баллов, с подпунктами)
                    bool isContainer = string.IsNullOrEmpty(pointsStr) && IsContainerRow(sheet, rowIdx);

                    int maxPointsNumeric = ParseMaxPoints(pointsStr);

                    var wi = new WorkItem
                    {
                        SectionId = currentSectionId,
                        ItemId = itemId,
                        Name = itemName.Trim(),
                        MaxPoints = pointsStr,
                        MaxPointsNumeric = maxPointsNumeric,
                        SortOrder = ++itemSortOrder
                    };

                    if (!isContainer)
                    {
                        workItems.Add(wi);

                        // Сохраняем данные строки для импорта баллов
                        itemRows.Add(new ItemRowData
                        {
                            SectionId = currentSectionId,
                            ItemId = itemId,
                            ItemName = itemName.Trim(),
                            RowIndex = rowIdx
                        });
                    }
                    else
                    {
                        // Контейнер — добавляем но без баллов
                        workItems.Add(wi);
                    }
                }

                // ── 4. Сохраняем разделы и пункты в БД ──
                foreach (var sec in sections)
                {
                    sectionRepo.InsertOrUpdate(sec);
                    result.SectionsCount++;
                }

                foreach (var wi in workItems)
                {
                    workItemRepo.InsertOrUpdate(wi);
                    result.WorkItemsCount++;
                }

                // ── 5. Импортируем баллы преподавателей ──
                var today = DateTime.Now;

                foreach (var itemData in itemRows)
                {
                    var row = sheet.GetRow(itemData.RowIndex);
                    if (row == null) continue;

                    // Находим WorkItem для получения MaxPoints
                    WorkItem wi = null;
                    foreach (var sec in sections)
                    {
                        if (sec.SectionId == itemData.SectionId)
                        {
                            var items = workItems.Where(w => w.SectionId == sec.SectionId && w.ItemId == itemData.ItemId);
                            wi = items.FirstOrDefault();
                            break;
                        }
                    }

                    int maxPerWork = wi?.MaxPointsNumeric ?? 0;

                    foreach (var kvp in teacherColumns)
                    {
                        int col = kvp.Key;
                        var teacher = kvp.Value;

                        var pointsCell = row.GetCell(col);
                        if (pointsCell == null) continue;

                        int totalPoints = GetCellInt(pointsCell);
                        if (totalPoints == 0) continue;

                        // Проверяем нет ли уже работ по этому пункту
                        var existingWorks = workRepo.GetByTeacher(teacher.TeacherId);
                        if (existingWorks.Any(w =>
                            w.SectionId == itemData.SectionId && w.ItemId == itemData.ItemId))
                            continue;

                        // Разбиваем на порции если maxPerWork > 0 и totalPoints > maxPerWork
                        var portions = SplitPoints(totalPoints, maxPerWork);

                        foreach (var portion in portions)
                        {
                            var work = new PlannedWork
                            {
                                WorkId = Guid.NewGuid().ToString(),
                                TeacherId = teacher.TeacherId,
                                SectionId = itemData.SectionId,
                                ItemId = itemData.ItemId,
                                WorkName = itemData.ItemName,
                                Points = portion,
                                DueDate = today,
                                Status = WorkStatus.Confirmed,
                                CreatedAt = today,
                                UpdatedAt = today
                            };

                            try
                            {
                                workRepo.Insert(work);
                                result.ScoresCount++;
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add(
                                    $"Row {itemData.RowIndex}, {teacher.ShortName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return result;
        }

        #region Helper methods
        /// <summary>
        /// Разбивает баллы на порции по максимуму за одну работу.
        /// 28 при макс 8 → [8, 8, 8, 4]
        /// 7 при макс 7 → [7]
        /// 15 при макс 0 → [15] (без ограничения)
        /// </summary>
        private List<int> SplitPoints(int totalPoints, int maxPerWork)
        {
            var result = new List<int>();

            if (maxPerWork <= 0 || totalPoints <= maxPerWork)
            {
                result.Add(totalPoints);
                return result;
            }

            int remaining = totalPoints;
            while (remaining > 0)
            {
                int portion = Math.Min(remaining, maxPerWork);
                result.Add(portion);
                remaining -= portion;
            }

            return result;
        }
        private class ItemRowData
        {
            public int SectionId { get; set; }
            public string ItemId { get; set; }
            public string ItemName { get; set; }
            public int RowIndex { get; set; }
        }

        private int DetectSectionHeader(string cellA, string cellB)
        {
            var text = (cellA + " " + cellB).Replace('\xa0', ' ').Trim();
            var match = Regex.Match(text, @"^(\d+)\.\s+\S");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return 0;
        }

        private string ExtractSectionName(string cellA, string cellB)
        {
            var text = string.IsNullOrEmpty(cellB)
                ? cellA : cellB;
            text = text.Replace('\xa0', ' ').Trim();
            text = Regex.Replace(text, @"^\d+\.\s+", "");
            return text.Trim();
        }

        private bool IsContainerRow(ISheet sheet, int rowIdx)
        {
            // Если следующая строка — подпункт (начинается с "-" или пробелов), это контейнер
            var nextRow = sheet.GetRow(rowIdx + 1);
            if (nextRow == null) return false;
            var nextB = GetCellString(nextRow.GetCell(1));
            var nextA = GetCellString(nextRow.GetCell(0));
            return string.IsNullOrEmpty(nextA) &&
                   (nextB.StartsWith("-") || nextB.StartsWith(" ") || nextB.StartsWith("\xa0"));
        }

        private string GetCellString(ICell cell)
        {
            if (cell == null) return "";
            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.String:
                    return cell.StringCellValue ?? "";
                case CellType.Formula:
                    try { return cell.NumericCellValue.ToString(); }
                    catch { return cell.StringCellValue ?? ""; }
                default:
                    return cell.ToString() ?? "";
            }
        }

        private int GetCellInt(ICell cell)
        {
            if (cell == null) return 0;
            try
            {
                switch (cell.CellType)
                {
                    case CellType.Numeric:
                        return (int)cell.NumericCellValue;
                    case CellType.String:
                        int.TryParse(cell.StringCellValue?.Trim(), out var v);
                        return v;
                    case CellType.Formula:
                        return (int)cell.NumericCellValue;
                    default:
                        return 0;
                }
            }
            catch { return 0; }
        }

        private string CleanPoints(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return raw.Replace("\n", "/").Replace("\r", "")
                .Replace("\xa0", "").Trim()
                .TrimEnd('/');
        }

        private int ParseMaxPoints(string pointsStr)
        {
            if (string.IsNullOrEmpty(pointsStr)) return 0;
            // "2/4/6/15" → берём максимум
            var parts = pointsStr.Split('/');
            int max = 0;
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var v))
                    max = Math.Max(max, v);
            }
            return max;
        }

        #endregion
    }
}