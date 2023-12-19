using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class UserAchievement
    {
        // PK, FK
        [Key]
        public string UserId { get; set; }

        // PK, FK
        [Key]
        public int AchievementId { get; set; }

        public DateTime UnlockDate { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Achievement? Achievement { get; set; }
    }
}
