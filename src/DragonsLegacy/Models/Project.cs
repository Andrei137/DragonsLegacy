using System.ComponentModel.DataAnnotations;


namespace DragonsLegacy.Models
{
    public class Project
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(25, ErrorMessage = "The name needs to have at most 25 characters.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        // FK
        public string? OrganizerId { get; set; }

        // The organizer
        public virtual ApplicationUser? Organizer { get; set; }

        // The project's tasks
        public virtual ICollection<Task>? Tasks { get; set; }

        // The project's teams
        public virtual ICollection<TeamProject>? TeamProjects { get; set; }

        // The project's users
        public virtual ICollection<UserProject>? UserProjects { get; set; }
    }
}