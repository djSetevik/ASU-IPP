using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ASUIPP.Core.Services
{
    public class ReportService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ReferenceRepository _refRepo;
        private readonly WorkRepository _workRepo;
        private readonly TeacherRepository _teacherRepo;
        private readonly SettingsRepository _settingsRepo;

        public ReportService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _refRepo = new ReferenceRepository(dbContext);
            _workRepo = new WorkRepository(dbContext);
            _teacherRepo = new TeacherRepository(dbContext);
            _settingsRepo = new SettingsRepository(dbContext);
        }

        public string GenerateSummaryReport(string outputPath)
        {
            var teachers = _teacherRepo.GetAll();
            var sections = _refRepo.GetAllSections();
            var allItems = _refRepo.GetAllWorkItems();
            var settings = _settingsRepo.GetAppSettings();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Сводный отчёт");

                ws.Cells[1, 1].Value = $"Сводный отчёт по кафедре \"{settings.DepartmentName}\"";
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 1].Style.Font.Size = 14;
                ws.Cells[2, 1].Value = $"{settings.SemesterNumber}-й семестр {settings.SemesterYear} уч. года";

                int headerRow = 4;
                ws.Cells[headerRow, 1].Value = "№";
                ws.Cells[headerRow, 2].Value = "Виды работ";
                ws.Cells[headerRow, 3].Value = "Баллы";

                for (int t = 0; t < teachers.Count; t++)
                {
                    ws.Cells[headerRow, 4 + t].Value = teachers[t].ShortName;
                    ws.Cells[headerRow, 4 + t].Style.TextRotation = 90;
                    ws.Cells[headerRow, 4 + t].Style.Font.Size = 9;
                }

                var headerRange = ws.Cells[headerRow, 1, headerRow, 3 + teachers.Count];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                int row = headerRow + 1;

                var teacherWorks = new Dictionary<string, List<PlannedWork>>();
                foreach (var teacher in teachers)
                    teacherWorks[teacher.TeacherId] = _workRepo.GetByTeacher(teacher.TeacherId);

                foreach (var section in sections)
                {
                    ws.Cells[row, 1].Value = $"{section.SectionId}.";
                    ws.Cells[row, 2].Value = section.Name;
                    ws.Cells[row, 1, row, 3 + teachers.Count].Style.Font.Bold = true;
                    ws.Cells[row, 1, row, 3 + teachers.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 3 + teachers.Count].Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.FromArgb(220, 230, 241));

                    for (int t = 0; t < teachers.Count; t++)
                    {
                        var rawPoints = teacherWorks[teachers[t].TeacherId]
                            .Where(w => w.SectionId == section.SectionId)
                            .Sum(w => w.Points);
                        // Лимит 50 за раздел
                        ws.Cells[row, 4 + t].Value = Math.Min(rawPoints, PointsLimits.MaxPerSection);
                    }
                    row++;

                    var items = allItems.Where(wi => wi.SectionId == section.SectionId)
                        .OrderBy(wi => wi.SortOrder).ToList();

                    foreach (var item in items)
                    {
                        ws.Cells[row, 1].Value = item.ItemId;
                        ws.Cells[row, 2].Value = item.Name;
                        ws.Cells[row, 3].Value = item.MaxPoints;
                        ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        for (int t = 0; t < teachers.Count; t++)
                        {
                            var points = teacherWorks[teachers[t].TeacherId]
                                .Where(w => w.SectionId == section.SectionId && w.ItemId == item.ItemId)
                                .Sum(w => w.Points);
                            if (points != 0)
                                ws.Cells[row, 4 + t].Value = points;
                        }
                        row++;
                    }
                }

                // Итого с лимитом 100
                ws.Cells[row, 2].Value = "ИТОГО";
                ws.Cells[row, 1, row, 3 + teachers.Count].Style.Font.Bold = true;
                ws.Cells[row, 1, row, 3 + teachers.Count].Style.Font.Size = 12;
                ws.Cells[row, 1, row, 3 + teachers.Count].Style.Border.Top.Style = ExcelBorderStyle.Double;

                for (int t = 0; t < teachers.Count; t++)
                {
                    int totalWithLimits = 0;
                    foreach (var section in sections)
                    {
                        var secSum = teacherWorks[teachers[t].TeacherId]
                            .Where(w => w.SectionId == section.SectionId)
                            .Sum(w => w.Points);
                        totalWithLimits += Math.Min(secSum, PointsLimits.MaxPerSection);
                    }
                    ws.Cells[row, 4 + t].Value = Math.Min(totalWithLimits, PointsLimits.MaxTotal);
                }

                ws.Column(1).Width = 6;
                ws.Column(2).Width = 55;
                ws.Column(3).Width = 10;
                for (int t = 0; t < teachers.Count; t++)
                    ws.Column(4 + t).Width = 5;

                ws.Cells[headerRow, 1, row, 3 + teachers.Count].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                ws.Cells[headerRow, 1, row, 3 + teachers.Count].Style.WrapText = true;
                ws.Cells[headerRow, 1, row, 3 + teachers.Count].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                var filePath = Path.Combine(outputPath,
                    $"Сводный_отчёт_{settings.DepartmentShortName}_{DateTime.Now:yyyy-MM-dd}.xlsx");
                package.SaveAs(new FileInfo(filePath));
                return filePath;
            }
        }

        public string GeneratePersonalReport(string teacherId, string outputPath)
        {
            var teacher = _teacherRepo.GetById(teacherId);
            if (teacher == null) throw new InvalidOperationException("Преподаватель не найден.");

            var sections = _refRepo.GetAllSections();
            var works = _workRepo.GetByTeacher(teacherId);
            var settings = _settingsRepo.GetAppSettings();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Индивидуальный отчёт");

                ws.Cells[1, 1].Value = $"Индивидуальный отчёт: {teacher.FullName}";
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 1].Style.Font.Size = 14;
                ws.Cells[2, 1].Value = $"Кафедра: {settings.DepartmentName}";
                ws.Cells[3, 1].Value = $"{settings.SemesterNumber}-й семестр {settings.SemesterYear} уч. года";

                int headerRow = 5;
                string[] headers = { "Раздел", "Пункт", "Название работы", "Баллы", "Статус", "Дата", "Файлы" };
                for (int c = 0; c < headers.Length; c++)
                {
                    ws.Cells[headerRow, c + 1].Value = headers[c];
                    ws.Cells[headerRow, c + 1].Style.Font.Bold = true;
                }

                var headerRange = ws.Cells[headerRow, 1, headerRow, headers.Length];
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 241));
                headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                int row = headerRow + 1;
                int grandTotal = 0;

                foreach (var section in sections)
                {
                    var sectionWorks = works.Where(w => w.SectionId == section.SectionId)
                        .OrderBy(w => w.ItemId).ToList();

                    if (!sectionWorks.Any()) continue;

                    ws.Cells[row, 1].Value = $"{section.SectionId}. {section.Name}";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1, row, headers.Length].Merge = true;
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, headers.Length].Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.FromArgb(240, 240, 240));

                    var rawSectionTotal = sectionWorks.Sum(w => w.Points);
                    var sectionTotal = Math.Min(rawSectionTotal, PointsLimits.MaxPerSection);
                    grandTotal += sectionTotal;

                    // Показываем с пометкой если превышен лимит
                    ws.Cells[row, headers.Length].Value = rawSectionTotal > PointsLimits.MaxPerSection
                        ? $"{sectionTotal} (из {rawSectionTotal})"
                        : sectionTotal.ToString();
                    row++;

                    foreach (var work in sectionWorks)
                    {
                        var workItem = _refRepo.GetWorkItem(work.SectionId, work.ItemId);

                        ws.Cells[row, 1].Value = $"{section.SectionId}. {section.Name}";
                        ws.Cells[row, 2].Value = workItem?.DisplayId ?? work.ItemId;
                        ws.Cells[row, 3].Value = work.WorkName;
                        ws.Cells[row, 4].Value = work.Points;
                        ws.Cells[row, 5].Value = StatusToString(work.Status);
                        ws.Cells[row, 6].Value = work.DueDate?.ToString("dd.MM.yyyy") ?? "";
                        ws.Cells[row, 7].Value = string.Join(", ",
                            work.AttachedFiles.Select(f => f.FileName));
                        row++;
                    }
                }

                // Итого с лимитом
                grandTotal = Math.Min(grandTotal, PointsLimits.MaxTotal);
                ws.Cells[row, 3].Value = "ИТОГО";
                ws.Cells[row, 3].Style.Font.Bold = true;
                ws.Cells[row, 4].Value = grandTotal;
                ws.Cells[row, 4].Style.Font.Bold = true;
                ws.Cells[row, 4].Style.Font.Size = 12;
                ws.Cells[row, 1, row, headers.Length].Style.Border.Top.Style = ExcelBorderStyle.Double;

                ws.Column(1).Width = 12;
                ws.Column(2).Width = 8;
                ws.Column(3).Width = 50;
                ws.Column(4).Width = 8;
                ws.Column(5).Width = 18;
                ws.Column(6).Width = 12;
                ws.Column(7).Width = 25;

                ws.Cells[headerRow, 1, row, headers.Length].Style.WrapText = true;
                ws.Cells[headerRow, 1, row, headers.Length].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                var safeName = Helpers.FileHelper.SanitizeFileName(teacher.ShortName.Replace(".", ""));
                var filePath = Path.Combine(outputPath,
                    $"Отчёт_{safeName}_{DateTime.Now:yyyy-MM-dd}.xlsx");
                package.SaveAs(new FileInfo(filePath));
                return filePath;
            }
        }

        private string StatusToString(WorkStatus status)
        {
            switch (status)
            {
                case WorkStatus.Planned: return "Запланирована";
                case WorkStatus.InProgress: return "Выполняется";
                case WorkStatus.Done: return "Ожидает подтверждения";
                case WorkStatus.Confirmed: return "Подтверждена";
                default: return status.ToString();
            }
        }
    }
}