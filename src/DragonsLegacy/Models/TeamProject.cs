using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class TeamProject
    {
        // PK, FK
        [Key]
        public int TeamId { get; set; }

        // PK, FK
        [Key]
        public int ProjectId { get; set; }

        public virtual Team? Team { get; set; }

        public virtual Project? Project { get; set; }
    }
}
