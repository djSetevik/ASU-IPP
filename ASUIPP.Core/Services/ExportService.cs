using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;

namespace ASUIPP.Core.Services
{
    /// <summary>
    /// Формирует ZIP-архив с данными преподавателя для передачи завкафедрой.
    /// </summary>
    public class ExportService
    {
        private readonly WorkRepository _workRepo;
        private readonly TeacherRepository _teacherRepo;
        private readonly SettingsRepository _settingsRepo;

        public ExportService(WorkRepository workRepo, TeacherRepository teacherRepo, SettingsRepository settingsRepo)
        {
            _workRepo = workRepo;
            _teacherRepo = teacherRepo;
            _settingsRepo = settingsRepo;
        }

        /// <summary>
        /// Экспортирует данные преподавателя в ZIP-архив.
        /// Возвращает путь к созданному файлу.
        /// </summary>
        public string Export(string teacherId, string outputDirectory)
        {
            var teacher = _teacherRepo.GetById(teacherId);
            if (teacher == null)
                throw new InvalidOperationException("Преподаватель не найден.");

            var settings = _settingsRepo.GetAppSettings();
            var works = _workRepo.GetByTeacher(teacherId);

            // Формируем пакет
            var package = new ExportPackage
            {
                ExportDate = DateTime.Now,
                Teacher = new ExportTeacher
                {
                    TeacherId = teacher.TeacherId,
                    FullName = teacher.FullName,
                    ShortName = teacher.ShortName
                },
                Semester = new ExportSemester
                {
                    Year = settings.SemesterYear,
                    Number = settings.SemesterNumber
                }
            };

            foreach (var work in works)
            {
                var exportWork = new ExportWork
                {
                    WorkId = work.WorkId,
                    SectionId = work.SectionId,
                    ItemId = work.ItemId,
                    WorkName = work.WorkName,
                    Points = work.Points,
                    DueDate = work.DueDate?.ToString("yyyy-MM-dd"),
                    Status = work.Status.ToString()
                };

                foreach (var file in work.AttachedFiles)
                {
                    exportWork.AttachedFiles.Add(new ExportFile
                    {
                        FileName = file.FileName,
                        RelativePath = $"files/{work.WorkId}/{file.FileName}"
                    });
                }

                package.Works.Add(exportWork);
            }

            // Создаём временную папку
            var tempDir = FileHelper.CreateTempDirectory();

            try
            {
                // data.json
                var json = JsonConvert.SerializeObject(package, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                File.WriteAllText(Path.Combine(tempDir, "data.json"), json, System.Text.Encoding.UTF8);

                // Копируем файлы
                var filesDir = Path.Combine(tempDir, "files");
                foreach (var work in works)
                {
                    foreach (var file in work.AttachedFiles)
                    {
                        var sourcePath = FileHelper.GetFullFilePath(file.FilePath);
                        if (!File.Exists(sourcePath)) continue;

                        var destDir = Path.Combine(filesDir, work.WorkId);
                        Directory.CreateDirectory(destDir);
                        File.Copy(sourcePath, Path.Combine(destDir, file.FileName), overwrite: true);
                    }
                }

                // Архивируем
                var safeName = FileHelper.SanitizeFileName(teacher.ShortName.Replace(".", ""));
                var zipName = $"АСУИПП_{safeName}_{DateTime.Now:yyyy-MM-dd}.zip";
                var zipPath = Path.Combine(outputDirectory, zipName);

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, false);
                return zipPath;
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempDir);
            }
        }
    }
}