using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class TaskCategory
    {
        // PK, FK
        [Key]
        public int TaskId { get; set; }

        // PK, FK
        [Key]
        public int CategoryId { get; set; }

        public virtual Task? Task { get; set; }

        public virtual Category? Category { get; set; }
    }
}
