using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class UserItem
    {
        // PK, FK
        [Key]
        public string UserId {  get; set; }

        // PK, FK
        [Key]
        public int ItemId { get; set; }

        public DateTime PurchaseDate { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Item? Item { get; set; } 
    }
}
