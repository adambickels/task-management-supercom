using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Core.Entities
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;

        // Navigation property for many-to-many relationship with TaskItems
        public ICollection<TaskItemTag> TaskItemTags { get; set; } = new List<TaskItemTag>();
    }
}
