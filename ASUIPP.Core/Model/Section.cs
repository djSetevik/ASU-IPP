using System.Collections.Generic;

namespace ASUIPP.Core.Models
{
    public class Section
    {
        public int SectionId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public List<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    }
}