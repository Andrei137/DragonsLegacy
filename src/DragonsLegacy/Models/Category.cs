using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class Category
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(15, ErrorMessage = "The name needs to have at most 15 characters.")]
        public string Name { get; set; }

        // The tasks that have the category
        public virtual ICollection<TaskCategory>? TaskCategories { get; set; }
    }
}
