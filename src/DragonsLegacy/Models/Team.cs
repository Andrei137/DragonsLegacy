using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonsLegacy.Models
{
    public class Team
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(25, ErrorMessage = "The name needs to have at most 25 characters.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        // FK
        public string? ManagerId { get; set; }

        // The team's manager
        public virtual ApplicationUser? Manager { get; set; }

        // A team has multiple users throughout time
        public virtual ICollection<TeamHistory>? TeamsHistory { get; set; }

        // The team's projects
        public virtual ICollection<TeamProject>? TeamProjects { get; set; }

        // The team's users
        public virtual ICollection<UserTeam>? UserTeams { get; set; }
    }
}