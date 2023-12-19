using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static void Initialize(IServiceProvider
    serviceProvider)
    {
        using (var context = new ApplicationDbContext(
        serviceProvider.GetRequiredService
        <DbContextOptions<ApplicationDbContext>>()))
        {
            // We check if there are any roles in the database
            // meaning that the code has been executed
            // If true, we stop so we don't insert the content again
            if (context.Roles.Any())
            {
                return;
            }

            // We only have 2 explicit roles
            // Admin and Registered User (User)
            context.Roles.AddRange(
            new IdentityRole
            {
                Id = "ca88c4c0-7be3-4665-a453-26958da56438",
                Name = "Admin",
                NormalizedName = "Admin".ToUpper()
            },
            new IdentityRole
            {
                Id = "ca88c4c0-7be3-4665-a453-26958da56439",
                Name = "User",
                NormalizedName = "User".ToUpper()
            }
            );

            var hasher = new PasswordHasher<ApplicationUser>();

            // We create a new user for each role
            // Including an implicit role (Organizer) which is an User which creates a team
            context.Users.AddRange(
            new ApplicationUser
            {
                Id = "ab35842e-ee42-423f-8f77-98f3b5fc80c1", // primary key
                UserName = "admin@dragon.com",
                NormalizedUserName = "ADMIN@DRAGON.COM",
                Email = "admin@dragon.com",
                NormalizedEmail = "ADMIN@DRAGON.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Admin13!")
            },
            new ApplicationUser
            {
                Id = "ab35842e-ee42-423f-8f77-98f3b5fc80c2", // primary key
                UserName = "organizer@dragon.com",
                NormalizedUserName = "ORGANIZER@DRAGON.COM",
                Email = "organizer@dragon.com",
                NormalizedEmail = "ORGANIZER@DRAGON.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Organizer13!")
            },
            new ApplicationUser
            {
                Id = "ab35842e-ee42-423f-8f77-98f3b5fc80c3", // primary key
                UserName = "user@dragon.com",
                NormalizedUserName = "USER@DRAGON.COM",
                Email = "user@dragon.com",
                NormalizedEmail = "USER@DRAGON.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "User13!")
            }
);
            // We insert the roles for each user
            context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    UserId = "ab35842e-ee42-423f-8f77-98f3b5fc80c1", // Admin
                    RoleId = "ca88c4c0-7be3-4665-a453-26958da56438" // Admin
                },
                new IdentityUserRole<string>
                {
                    // The organizer role is implicit
                    UserId = "ab35842e-ee42-423f-8f77-98f3b5fc80c2", // Organizer
                    RoleId = "ca88c4c0-7be3-4665-a453-26958da56439" // User
                },
                new IdentityUserRole<string>
                {
                    RoleId = "ca88c4c0-7be3-4665-a453-26958da56439", // User
                    UserId = "ab35842e-ee42-423f-8f77-98f3b5fc80c3" // User
                }
            );
            context.SaveChanges();
        }
    }
}