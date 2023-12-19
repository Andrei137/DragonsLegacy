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

        // The date the user unlocked the achievement
        public DateTime UnlockDate { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Achievement? Achievement { get; set; }
    }
}
