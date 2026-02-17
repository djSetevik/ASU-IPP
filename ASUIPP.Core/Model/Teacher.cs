using System;

namespace ASUIPP.Core.Models
{
    public class Teacher
    {
        public string TeacherId { get; set; }    // GUID как строка
        public string FullName { get; set; }      // "Выплавень Владимир Сергеевич"
        public string ShortName { get; set; }     // "Выплавень В.С."
        public bool IsHead { get; set; }
        public DateTime CreatedAt { get; set; }

        public Teacher()
        {
            TeacherId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
        }
    }
}