using System.ComponentModel.DataAnnotations;

namespace DragonsLegacy.Models
{
    public class Achievement
    {
        // PK
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(25, ErrorMessage = "The name needs to have at most 25 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(100, ErrorMessage = "The name needs to have at most 100 characters.")]
        public string Description { get; set; }

        // The user receives experience points for doing an achievement
        [Required(ErrorMessage = "Experience points are required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than 0")]
        public int ExperiencePoints { get; set; } = 50;

        [Required(ErrorMessage = "Coins are required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than 0")]
        // The user could receive coins for doing an achievement
        public int Coins { get; set; } = 100;

        // The users who unlocked the achievement
        public virtual ICollection<UserAchievement>? UserAchievements { get; set; }
    }
}
