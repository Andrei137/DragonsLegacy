using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArticlesApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db           = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            return View();
        }

        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            var roleNames       = await _userManager.GetRolesAsync(user);
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First();
            ViewBag.UserRole    = db.Roles.Find(currentUserRole).Name;

            return View(user);
        }

        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            user.AllRoles        = GetAllRoles();
            var roleNames        = await _userManager.GetRolesAsync(user);
            var currentUserRole  = _roleManager.Roles
                                               .Where(r => roleNames.Contains(r.Name))
                                               .Select(r => r.Id)
                                               .First();
            ViewBag.UserRole     = currentUserRole;

            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);
            user.AllRoles        = GetAllRoles();

            if (ModelState.IsValid)
            {
                user.UserName    = newData.UserName;
                user.Email       = newData.Email;
                user.FirstName   = newData.FirstName;
                user.LastName    = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;
                var roles        = db.Roles.ToList();

                foreach (var role in roles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }

                var roleName = await _roleManager.FindByIdAsync(newRole);
                await _userManager.AddToRoleAsync(user, roleName.ToString());

                db.SaveChanges();
                
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                         .Include("Comments")
                         .Where(u => u.Id == id)
                         .First();

            // Delete the user's comments
            if (user.Comments != null)
            {
                foreach (var comment in user.Comments)
                {
                    db.Comments.Remove(comment);
                }
            }

            // Delete the user's tasks
            if (user.Tasks != null)
            {
                foreach (var task in user.Tasks)
                {
                    db.Tasks.Remove(task);
                }
            }

            // Delete the user's achievements
            if (user.UserAchievements != null)
            {
                foreach (var achievement in user.UserAchievements)
                {
                    db.UserAchievements.Remove(achievement);
                }
            }

            // Delete the user's items
            if (user.UserItems != null)
            {
                foreach (var item in user.UserItems)
                {
                    db.UserItems.Remove(item);
                }
            }

            replaceOrganizerAndManager(id);

            db.ApplicationUsers.Remove(user);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        private void replaceOrganizerAndManager(string id)
        {
            var teams = db.Teams
                          .Where(t => t.ManagerId == id)
                          .ToList();

            // The new manager will be the one with the most experience points from that team
            foreach (var team in teams)
            {
                var usersFromTeam = db.Users
                                      .Include("UserTeams")
                                      .Where(u => u.UserTeams.Any(ut => ut.TeamId == team.Id) && u.Id != team.ManagerId)
                                      .ToList();

                var newManager = usersFromTeam.OrderByDescending(u => u.ExperiencePoints)
                                              .First();

                team.ManagerId = newManager.Id;
            }

            var projects = db.Projects
                            .Where(p => p.OrganizerId == id)
                            .ToList();

            // The new organizer will be the one with the most experience points from that project
            foreach (var project in projects)
            {
                var usersFromProject = db.Users
                                         .Include("UserProjects")
                                         .Where(u => u.UserProjects.Any(up => up.ProjectId == project.Id) && u.Id != project.OrganizerId)
                                         .ToList();

                var newOrganizer = usersFromProject.OrderByDescending(u => u.ExperiencePoints)
                                                   .First();

                project.OrganizerId = newOrganizer.Id;
            }
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }
    }
}
