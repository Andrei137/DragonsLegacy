using System.ComponentModel.DataAnnotations;

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

        // We will make it required in the future
        // After we implement upload
        public string? Multimedia { get; set; }

        [Required(ErrorMessage = "Experience points are required")]
        public int ExperiencePoints { get; set; }

        public int Coins { get; set; } = 0;

        // FK
        public string UserId { get; set; }

        // FK
        public int ProjectId {  get; set; }

        // The task belongs to an user
        public virtual ApplicationUser? User { get; set; }

        public virtual Project? Project { get; set; }

        // The task has several comments
        public virtual ICollection<Comment>? Comments { get; set; }

        // The task has several categories
        public virtual ICollection<TaskCategory>? TaskCategories { get; set; }
    }
}
