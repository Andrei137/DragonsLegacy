using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonsLegacy.Models
{
    public class Task
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(25, ErrorMessage = "The name needs to have at most 25 characters.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public int Priority { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Deadline is required")]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "Starting date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Multimedia { get; set; }

        [Required(ErrorMessage = "Experience points are required")]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter a value bigger or equal to 0")]
        public int ExperiencePoints { get; set; } = 20;

        [Required(ErrorMessage = "Coins are required")]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter a value bigger or equal to 0")]
        public int Coins { get; set; } = 50;

        // FK
        public string UserId { get; set; }

        // FK
        public int ProjectId {  get; set; }

        // The task belongs to an user
        public virtual ApplicationUser? User { get; set; }

        // The task belongs to a project
        public virtual Project? Project { get; set; }

        // The task has several comments
        public virtual ICollection<Comment>? Comments { get; set; }

        // The task has several categories
        public virtual ICollection<TaskCategory>? TaskCategories { get; set; }

        // List of selected categories
        [NotMapped]
        public int[]? SelectedCategories { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? AllUsers { get; set; }
    }
}
