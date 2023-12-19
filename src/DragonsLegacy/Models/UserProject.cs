using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class UserProject
    {
        // PK, FK
        [Key]
        public string UserId { get; set; }

        // PK, FK
        [Key]
        public int ProjectId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Project? Project { get; set; }
    }
}
