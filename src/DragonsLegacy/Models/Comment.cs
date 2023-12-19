using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonsLegacy.Models
{
    public class Comment
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        // FK
        public string UserId { get; set; }

        // FK
        public int? TaskId { get; set; }

        // The comment belongs to an user
        public virtual ApplicationUser? User { get; set; }

        // The comment belongs to a taskW
        public virtual Task? Task { get; set; }
    }
}
