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

        // The item's photo
        // Might change to a list of photos
        public string? Multimedia { get; set; }

        // The price of the item
        [Required(ErrorMessage = "Price is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than 1")]
        public int Price { get; set; } = 1;

        // The stock
        [Required(ErrorMessage = "Number of items is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter a value bigger or equal to 0")]
        public int NumberOfItems { get; set; } = 0;

        // The users who bought the item
        public virtual ICollection<UserItem>? UserItems { get; set; }
    }
}
