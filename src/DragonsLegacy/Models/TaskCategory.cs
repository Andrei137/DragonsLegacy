using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class TaskCategory
    {
        [Key]
        public int TaskId { get; set; }

        [Key]
        public int CategoryId { get; set; }

        public virtual Task? Task { get; set; }

        public virtual Category? Category { get; set; }
    }
}
