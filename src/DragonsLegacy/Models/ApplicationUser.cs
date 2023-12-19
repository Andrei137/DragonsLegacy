using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonsLegacy.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateTime? BirthDate { get; set; }

        // Every user starts with 0
        public int ExperiencePoints { get; set; } = 0;

        // Every user starts with 0
        public int Coins { get; set; } = 0;

        // The user's comments 
        public virtual ICollection<Comment>? Comments { get; set; }

        // The user's tasks
        public virtual ICollection<Task>? Tasks { get; set; }

        // History of the user's teams
        public virtual ICollection<TeamHistory>? TeamsHistory { get; set; }

        // The user's achievements
        public virtual ICollection<UserAchievement>? UserAchievements { get; set; }

        // The user's bought items
        public virtual ICollection<UserItem>? UserItems { get; set; }

            // The user's teams
        public virtual ICollection<UserTeam>? UserTeams { get; set; }

        // The user's projects
        public virtual ICollection<UserProject>? UserProjects { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}