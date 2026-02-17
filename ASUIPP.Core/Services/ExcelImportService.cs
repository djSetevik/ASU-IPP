using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ASUIPP.Core.Services
{
    public class ExcelImportService
    {
        private readonly ReferenceRepository _referenceRepo;

        public ExcelImportService(DatabaseContext context)
        {
            _referenceRepo = new ReferenceRepository(context);
        }

        public int ImportReference(string filePath)
        {
            var workbook = OpenWorkbook(filePath);
            var sheet = workbook.GetSheet("Показатели") ?? workbook.GetSheetAt(0);

            _referenceRepo.ClearAll();

            var rawRows = new List<RawRow>();
            for (int rowIdx = 0; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                rawRows.Add(new RawRow
                {
                    RowIndex = rowIdx,
                    ColA = GetCellString(row, 0),
                    ColB = GetCellString(row, 1),
                    ColC = GetCellString(row, 2)
                });
            }

            int currentSectionId = 0;
            int sortOrder = 0;
            int itemCount = 0;
            string currentMainItemNumber = null;
            int subLetterIndex = 0;

            for (int i = 0; i < rawRows.Count; i++)
            {
                var r = rawRows[i];
                var colA = NormalizeSpaces(r.ColA);
                var colB = NormalizeSpaces(r.ColB);
                var colC = NormalizeSpaces(r.ColC);

                // ── Проверяем заголовок раздела в ОБЕИХ колонках ──
                // Раздел 1 лежит в колонке B, разделы 2-5 в колонке A
                int detectedSection = TryParseSectionHeader(colA);
                string sectionName = null;

                if (detectedSection > 0)
                {
                    // Заголовок в колонке A (разделы 2-5)
                    sectionName = ExtractSectionName(colA);
                }
                else
                {
                    detectedSection = TryParseSectionHeader(colB);
                    if (detectedSection > 0)
                    {
                        // Заголовок в колонке B (раздел 1)
                        sectionName = ExtractSectionName(colB);
                    }
                }

                if (detectedSection > 0 && sectionName != null)
                {
                    currentSectionId = detectedSection;
                    _referenceRepo.InsertSection(new Section
                    {
                        SectionId = currentSectionId,
                        Name = sectionName,
                        SortOrder = currentSectionId
                    });
                    currentMainItemNumber = null;
                    subLetterIndex = 0;
                    continue;
                }

                if (currentSectionId == 0) continue;

                // Пропускаем служебные строки
                if (colA == "№" || colA == "Итого") continue;
                if (colB.StartsWith("Итого", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(colB) && string.IsNullOrEmpty(colA)) continue;
                if (string.IsNullOrEmpty(colB)) continue;

                // ── Строка с номером в A — основной пункт ──
                var numId = ParseNumericId(colA);
                if (numId != null)
                {
                    currentMainItemNumber = numId;
                    subLetterIndex = 0;

                    if (!string.IsNullOrEmpty(colC))
                    {
                        sortOrder++;
                        _referenceRepo.InsertWorkItem(new WorkItem
                        {
                            SectionId = currentSectionId,
                            ItemId = numId,
                            Name = CleanText(colB),
                            MaxPoints = CleanPoints(colC),
                            MaxPointsNumeric = PointsValidator.ParseMaxPoints(CleanPoints(colC)),
                            SortOrder = sortOrder
                        });
                        itemCount++;
                    }
                    else
                    {
                        // Пункт без баллов — контейнер для подпунктов
                        bool hasSubItems = LookAheadForSubItems(rawRows, i + 1);
                        if (!hasSubItems)
                        {
                            sortOrder++;
                            _referenceRepo.InsertWorkItem(new WorkItem
                            {
                                SectionId = currentSectionId,
                                ItemId = numId,
                                Name = CleanText(colB),
                                MaxPoints = "—",
                                MaxPointsNumeric = null,
                                SortOrder = sortOrder
                            });
                            itemCount++;
                        }
                    }
                    continue;
                }

                // ── Подпункт (A пуст, есть баллы в C) ──
                if (string.IsNullOrEmpty(colA.Trim()) && !string.IsNullOrEmpty(colC) && currentMainItemNumber != null)
                {
                    subLetterIndex++;
                    var subId = $"{currentMainItemNumber}{GetSubSuffix(subLetterIndex)}";

                    sortOrder++;
                    _referenceRepo.InsertWorkItem(new WorkItem
                    {
                        SectionId = currentSectionId,
                        ItemId = subId,
                        Name = CleanText(colB),
                        MaxPoints = CleanPoints(colC),
                        MaxPointsNumeric = PointsValidator.ParseMaxPoints(CleanPoints(colC)),
                        SortOrder = sortOrder
                    });
                    itemCount++;
                }
            }

            return itemCount;
        }

        /// <summary>
        /// Пытается распарсить заголовок раздела из строки.
        /// Формат: "1.     Учебно-методическая работа" или "2.\xa0\xa0\xa0\xa0\xa0 Научно-..."
        /// Возвращает номер раздела (1-5) или 0 если не заголовок.
        /// </summary>
        private int TryParseSectionHeader(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            // Паттерн: цифра, точка, несколько пробелов/\xa0, текст
            var match = Regex.Match(text.Trim(), @"^(\d+)\.\s{2,}(.+)$");
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out var num) && num >= 1 && num <= 10)
                    return num;
            }
            return 0;
        }

        private string ExtractSectionName(string text)
        {
            var match = Regex.Match(text.Trim(), @"^\d+\.\s{2,}(.+)$");
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        /// <summary>
        /// Заменяет неразрывные пробелы (\xa0) на обычные.
        /// </summary>
        private string NormalizeSpaces(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace('\u00A0', ' ');
        }

        private bool LookAheadForSubItems(List<RawRow> rows, int startIndex)
        {
            for (int j = startIndex; j < rows.Count; j++)
            {
                var colA = NormalizeSpaces(rows[j].ColA).Trim();
                var colB = NormalizeSpaces(rows[j].ColB).Trim();
                var colC = NormalizeSpaces(rows[j].ColC).Trim();

                if (string.IsNullOrEmpty(colB) && string.IsNullOrEmpty(colA)) continue;

                // Следующий заголовок раздела — стоп
                if (TryParseSectionHeader(colA) > 0 || TryParseSectionHeader(colB) > 0)
                    return false;

                // Следующий основной пункт с номером — стоп
                if (ParseNumericId(colA) != null)
                    return false;

                if (colA.Trim() == "Итого" || colB.StartsWith("Итого"))
                    return false;

                // Подпункт: A пуст, C не пуст
                if (string.IsNullOrEmpty(colA) && !string.IsNullOrEmpty(colC))
                    return true;
            }
            return false;
        }

        public List<DepartmentInfo> ImportDepartments(string filePath)
        {
            var workbook = OpenWorkbook(filePath);
            var sheet = workbook.GetSheet("Кафедры");
            if (sheet == null) return new List<DepartmentInfo>();

            var result = new List<DepartmentInfo>();
            for (int rowIdx = 1; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                var fullName = GetCellString(row, 1);
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                result.Add(new DepartmentInfo
                {
                    FullName = fullName.Trim(),
                    ShortName = GetCellString(row, 4).Trim(),
                    HeadFullName = GetCellString(row, 2).Trim(),
                    HeadTitle = GetCellString(row, 3).Trim()
                });
            }
            return result;
        }

        public SemesterInfo ParseSemesterInfo(string filePath)
        {
            var workbook = OpenWorkbook(filePath);
            var sheet = workbook.GetSheet("Показатели") ?? workbook.GetSheetAt(0);
            var row0 = sheet.GetRow(0);
            if (row0 == null) return new SemesterInfo();

            var semNumber = GetCellString(row0, 4);
            var year = GetCellString(row0, 8);

            int sem = 0;
            if (double.TryParse(semNumber, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var semDbl))
                sem = (int)semDbl;

            return new SemesterInfo
            {
                Year = year?.Trim() ?? "",
                Number = sem
            };
        }

        private IWorkbook OpenWorkbook(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Path.GetExtension(filePath).ToLowerInvariant() == ".xlsx"
                    ? (IWorkbook)new XSSFWorkbook(fs)
                    : new HSSFWorkbook(fs);
            }
        }

        private string GetCellString(IRow row, int colIdx)
        {
            var cell = row.GetCell(colIdx);
            if (cell == null) return "";

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue ?? "";
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case CellType.Formula:
                    try { return cell.StringCellValue ?? ""; }
                    catch { return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture); }
                default:
                    return "";
            }
        }

        private string ParseNumericId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var trimmed = raw.Trim();

            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                if (num == Math.Floor(num))
                    return ((int)num).ToString();
                return num.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            }
            return null;
        }

        private string GetSubSuffix(int index)
        {
            var letters = "абвгдежзиклмнопрстуфхцчшщэюя";
            return index >= 1 && index <= letters.Length
                ? letters[index - 1].ToString()
                : index.ToString();
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        private string CleanPoints(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var result = raw.Replace("\n", "").Replace("\r", "");
            result = Regex.Replace(result, @"\s*/\s*", "/");
            return result.Trim();
        }

        private class RawRow
        {
            public int RowIndex { get; set; }
            public string ColA { get; set; }
            public string ColB { get; set; }
            public string ColC { get; set; }
        }
    }

    public class DepartmentInfo
    {
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string HeadFullName { get; set; }
        public string HeadTitle { get; set; }
    }

    public class SemesterInfo
    {
        public string Year { get; set; } = "";
        public int Number { get; set; }
    }
}