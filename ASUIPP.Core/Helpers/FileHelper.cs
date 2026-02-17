using System;
using System.IO;
using System.Linq;

namespace ASUIPP.Core.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// Корневая папка данных: %AppData%\ASUIPP
        /// </summary>
        public static string AppDataRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ASUIPP");

        /// <summary>
        /// Папка для подтверждающих файлов
        /// </summary>
        public static string FilesRoot => Path.Combine(AppDataRoot, "files");

        /// <summary>
        /// Папка для временных операций (экспорт/импорт)
        /// </summary>
        public static string TempRoot => Path.Combine(AppDataRoot, "temp");

        /// <summary>
        /// Возвращает путь для хранения файла конкретной работы:
        /// files/{teacherId}/{workId}/
        /// </summary>
        public static string GetWorkFilesDir(string teacherId, string workId)
        {
            var dir = Path.Combine(FilesRoot, teacherId, workId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Копирует файл в хранилище работы, возвращает относительный путь.
        /// </summary>
        public static string CopyFileToStorage(string sourceFilePath, string teacherId, string workId)
        {
            var dir = GetWorkFilesDir(teacherId, workId);
            var fileName = Path.GetFileName(sourceFilePath);

            // Если файл с таким именем уже есть — добавляем суффикс
            var destPath = Path.Combine(dir, fileName);
            var counter = 1;
            while (File.Exists(destPath))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                destPath = Path.Combine(dir, $"{name}_{counter}{ext}");
                counter++;
            }

            File.Copy(sourceFilePath, destPath);

            // Относительный путь для хранения в БД
            return Path.Combine(teacherId, workId, Path.GetFileName(destPath));
        }

        /// <summary>
        /// Полный путь к файлу по относительному пути из БД.
        /// </summary>
        public static string GetFullFilePath(string relativePath)
        {
            return Path.Combine(FilesRoot, relativePath);
        }

        /// <summary>
        /// Удаляет файл из хранилища по относительному пути.
        /// </summary>
        public static void DeleteStoredFile(string relativePath)
        {
            var fullPath = GetFullFilePath(relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// Удаляет всю папку работы (все прикреплённые файлы).
        /// </summary>
        public static void DeleteWorkFiles(string teacherId, string workId)
        {
            var dir = Path.Combine(FilesRoot, teacherId, workId);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        /// <summary>
        /// Расширение файла без точки, в нижнем регистре.
        /// </summary>
        public static string GetFileType(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return string.IsNullOrEmpty(ext) ? "" : ext.TrimStart('.').ToLowerInvariant();
        }

        /// <summary>
        /// Создаёт временную папку и возвращает путь. Вызывающий отвечает за очистку.
        /// </summary>
        public static string CreateTempDir()
        {
            var dir = Path.Combine(TempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Безопасно удаляет временную папку.
        /// </summary>
        public static void CleanupTempDir(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // Не критично если temp не удалился
            }
        }

        /// <summary>
        /// Генерирует короткое ФИО: "Выплавень Владимир Сергеевич" → "Выплавень В.С."
        /// </summary>
        public static string ToShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0];

            var surname = parts[0];
            var initials = string.Join("",
                parts.Skip(1).Select(p => p[0] + "."));

            return $"{surname} {initials}";
        }

        // LINQ Skip/Select требует using System.Linq — добавлен ниже
        /// <summary>
        /// Создаёт временную папку. Алиас для CreateTempDir().
        /// </summary>
        public static string CreateTempDirectory()
        {
            return CreateTempDir();
        }

        /// <summary>
        /// Безопасно удаляет директорию. Алиас для CleanupTempDir().
        /// </summary>
        public static void SafeDeleteDirectory(string path)
        {
            CleanupTempDir(path);
        }

        /// <summary>
        /// Убирает из имени файла недопустимые символы.
        /// </summary>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "file";

            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(clean) ? "file" : clean;
        }
    }
}