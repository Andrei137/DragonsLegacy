using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class UserTeam
    {
        // PK, FK
        [Key]
        public string UserId { get; set; }

        // PK, FK
        [Key]
        public int TeamId { get; set; }

        public virtual ApplicationUser? User {  get; set; }
        
        public virtual Team? Team { get; set; }
    }
}
