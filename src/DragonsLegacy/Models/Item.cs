using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class Item
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "The name needs to have at most 50 characters.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        public int Price { get; set; }

        [Required(ErrorMessage = "Number of items is required")]
        public int NumberOfItems { get; set; }

        // The users who bought the item
        public virtual ICollection<UserItem>? UserItems { get; set; }
    }
}
