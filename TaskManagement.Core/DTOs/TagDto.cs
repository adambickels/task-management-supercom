using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Core.DTOs
{
    public class TagDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
