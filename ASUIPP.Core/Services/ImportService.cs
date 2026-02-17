using System;
using System.IO;
using System.IO.Compression;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;
using Newtonsoft.Json;

namespace ASUIPP.Core.Services
{
    public class ImportService
    {
        private readonly DatabaseContext _dbContext;
        private readonly TeacherRepository _teacherRepo;
        private readonly WorkRepository _workRepo;

        public ImportService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _teacherRepo = new TeacherRepository(dbContext);
            _workRepo = new WorkRepository(dbContext);
        }

        public void Import(string zipPath)
        {
            var tempDir = FileHelper.CreateTempDirectory();

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempDir);

                var jsonPath = Path.Combine(tempDir, "data.json");
                if (!File.Exists(jsonPath))
                    throw new FileNotFoundException("Файл data.json не найден в архиве.");

                var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var package = JsonConvert.DeserializeObject<ExportPackage>(json);

                if (package?.Teacher == null)
                    throw new InvalidOperationException("Некорректный формат данных.");

                // Ищем или создаём преподавателя
                var teacher = _teacherRepo.GetById(package.Teacher.TeacherId);
                if (teacher == null)
                {
                    teacher = new Teacher
                    {
                        TeacherId = package.Teacher.TeacherId,
                        FullName = package.Teacher.FullName,
                        ShortName = package.Teacher.ShortName,
                        IsHead = false
                    };
                    _teacherRepo.Insert(teacher);
                }

                // Импортируем работы
                foreach (var exportWork in package.Works)
                {
                    var existing = _workRepo.GetById(exportWork.WorkId);

                    WorkStatus status = WorkStatus.Planned;
                    Enum.TryParse(exportWork.Status, out status);

                    DateTime? dueDate = null;
                    if (!string.IsNullOrEmpty(exportWork.DueDate))
                        DateTime.TryParse(exportWork.DueDate, out var dd);

                    if (!string.IsNullOrEmpty(exportWork.DueDate) &&
                        DateTime.TryParse(exportWork.DueDate, out var parsedDate))
                        dueDate = parsedDate;

                    if (existing != null)
                    {
                        existing.SectionId = exportWork.SectionId;
                        existing.ItemId = exportWork.ItemId;
                        existing.WorkName = exportWork.WorkName;
                        existing.Points = exportWork.Points;
                        existing.DueDate = dueDate;
                        existing.Status = status;
                        _workRepo.Update(existing);
                    }
                    else
                    {
                        var work = new PlannedWork
                        {
                            WorkId = exportWork.WorkId,
                            TeacherId = package.Teacher.TeacherId,
                            SectionId = exportWork.SectionId,
                            ItemId = exportWork.ItemId,
                            WorkName = exportWork.WorkName,
                            Points = exportWork.Points,
                            DueDate = dueDate,
                            Status = status
                        };
                        _workRepo.Insert(work);
                    }

                    // Копируем прикреплённые файлы
                    foreach (var exportFile in exportWork.AttachedFiles)
                    {
                        var sourcePath = Path.Combine(tempDir, exportFile.RelativePath);
                        if (!File.Exists(sourcePath)) continue;

                        var destDir = FileHelper.GetWorkFilesDir(package.Teacher.TeacherId, exportWork.WorkId);
                        var destPath = Path.Combine(destDir, exportFile.FileName);

                        if (!File.Exists(destPath))
                        {
                            File.Copy(sourcePath, destPath);

                            var af = new AttachedFile
                            {
                                WorkId = exportWork.WorkId,
                                FileName = exportFile.FileName,
                                FilePath = Path.Combine(package.Teacher.TeacherId, exportWork.WorkId, exportFile.FileName),
                                FileType = FileHelper.GetFileType(exportFile.FileName)
                            };
                            _workRepo.InsertFile(af);
                        }
                    }
                }
            }
            finally
            {
                FileHelper.SafeDeleteDirectory(tempDir);
            }
        }
    }
}