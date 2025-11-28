using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.DTOs
{
    public class TaskItemDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public TaskPriority Priority { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telephone is required")]
        [Phone(ErrorMessage = "Invalid telephone number")]
        [StringLength(20, ErrorMessage = "Telephone cannot exceed 20 characters")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one tag is required")]
        [MinLength(1, ErrorMessage = "At least one tag is required")]
        public List<int> TagIds { get; set; } = new List<int>();

        public List<TagDto> Tags { get; set; } = new List<TagDto>();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
