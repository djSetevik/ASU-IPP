namespace ASUIPP.Core.Models
{
    public class AttachedFile
    {
        public string FileId { get; set; }       // GUID
        public string WorkId { get; set; }        // FK → PlannedWork
        public string FileName { get; set; }      // "scan1.jpg"
        public string FilePath { get; set; }      // Относительный путь: "{TeacherId}/{WorkId}/scan1.jpg"
        public string FileType { get; set; }      // "jpg", "png", "pdf"

        public AttachedFile()
        {
            FileId = System.Guid.NewGuid().ToString();
        }
    }
}