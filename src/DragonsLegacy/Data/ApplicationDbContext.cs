using DragonsLegacy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Task = DragonsLegacy.Models.Task;

namespace DragonsLegacy.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamHistory> TeamsHistory { get; set; }
        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TeamProject> TeamProjects { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<UserItem> UserItems { get; set; }
        public DbSet<UserProject> UserProjects { get; set; }
        public DbSet<UserTeam> UserTeams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define composite primary keys
            modelBuilder.Entity<TaskCategory>()
                        .HasKey(tc => new { tc.TaskId, tc.CategoryId });

            modelBuilder.Entity<TeamHistory>()
                        .HasKey(th => new { th.UserId, th.StartDate });

            modelBuilder.Entity<TeamProject>()
                        .HasKey(tp => new { tp.TeamId, tp.ProjectId });

            modelBuilder.Entity<UserAchievement>()
                        .HasKey(ua => new { ua.UserId, ua.AchievementId });

            modelBuilder.Entity<UserItem>()
                        .HasKey(ui => new { ui.UserId, ui.ItemId });

            modelBuilder.Entity<UserProject>()
                        .HasKey(up => new { up.UserId, up.ProjectId });

            modelBuilder.Entity<UserTeam>()
                        .HasKey(ut => new { ut.UserId, ut.TeamId });

            // Define relationships with other models (FK)
            modelBuilder.Entity<TaskCategory>()
                        .HasOne(tc => tc.Task)
                        .WithMany(tc => tc.TaskCategories)
                        .HasForeignKey(tc => tc.TaskId);

            modelBuilder.Entity<TaskCategory>()
                        .HasOne(tc => tc.Category)
                        .WithMany(tc => tc.TaskCategories)
                        .HasForeignKey(tc => tc.CategoryId);

            modelBuilder.Entity<TeamProject>()
                        .HasOne(tp => tp.Team)
                        .WithMany(tp => tp.TeamProjects)
                        .HasForeignKey(tp => tp.TeamId);

            modelBuilder.Entity<TeamProject>()
                        .HasOne(tp => tp.Project)
                        .WithMany(tp => tp.TeamProjects)
                        .HasForeignKey(tp => tp.ProjectId);

            modelBuilder.Entity<UserAchievement>()
                        .HasOne(ua => ua.User)
                        .WithMany(ua => ua.UserAchievements)
                        .HasForeignKey(ua => ua.UserId);

            modelBuilder.Entity<UserAchievement>()
                        .HasOne(ua => ua.Achievement)
                        .WithMany(ua => ua.UserAchievements)
                        .HasForeignKey(ua => ua.AchievementId);

            modelBuilder.Entity<UserItem>()
                        .HasOne(ui => ui.User)
                        .WithMany(ui => ui.UserItems)
                        .HasForeignKey(ui => ui.UserId);

            modelBuilder.Entity<UserItem>()
                        .HasOne(ui => ui.Item)
                        .WithMany(ui => ui.UserItems)
                        .HasForeignKey(ui => ui.ItemId);

            modelBuilder.Entity<UserProject>()
                        .HasOne(up => up.User)
                        .WithMany(up => up.UserProjects)
                        .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserProject>()
                        .HasOne(up => up.Project)
                        .WithMany(up => up.UserProjects)
                        .HasForeignKey(up => up.ProjectId);

            modelBuilder.Entity<UserTeam>()
                        .HasOne(ut => ut.User)
                        .WithMany(ut => ut.UserTeams)
                        .HasForeignKey(ut => ut.UserId);

            modelBuilder.Entity<UserTeam>()
                        .HasOne(ut => ut.Team)
                        .WithMany(ut => ut.UserTeams)
                        .HasForeignKey(ut => ut.TeamId);

            modelBuilder.Entity<Comment>()
                        .HasOne(c => c.Task)
                        .WithMany(t => t.Comments)
                        .HasForeignKey(c => c.TaskId)
                        .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Comment>()
                        .HasOne(c => c.User)
                        .WithMany(u => u.Comments)
                        .HasForeignKey(c => c.UserId)
                        .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Project>()
                        .HasOne(p => p.Organizer)
                        .WithMany(o => o.OrganizedProjects)
                        .HasForeignKey(p => p.OrganizerId);

            modelBuilder.Entity<Team>()
                        .HasOne(t => t.Manager)
                        .WithMany(m => m.ManagedTeams)
                        .HasForeignKey(t => t.ManagerId);

            modelBuilder.Entity<Task>()
                        .HasOne(t => t.Project)
                        .WithMany(p => p.Tasks)
                        .HasForeignKey(t => t.ProjectId);
        }
    }
}
