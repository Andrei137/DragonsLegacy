using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class TeamHistory
    {
        // PK, FK
        [Key]
        public string UserId { get; set; }

        // PK
        [Key]
        public DateTime StartDate { get; set; }

        // FK
        [Required(ErrorMessage = "Team Id is required")]
        public int TeamId { get; set; }

        public DateTime? EndDate { get; set; }

        // This is the object from class User to which this TeamHistory is related
        public virtual ApplicationUser? User { get; set; }

        // This is the object from class Team to which this TeamHistory is related
        public virtual Team? Team { get; set; }

    }
}