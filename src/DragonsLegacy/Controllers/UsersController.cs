using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ArticlesApp.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController
        (
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
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert   = TempData["messageType"];
            }

            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            user.AllRoles        = GetAllRoles(user);
            ViewBag.IsAdmin      = _userManager.IsInRoleAsync(user, "Admin").Result;
            var roleNames        = await _userManager.GetRolesAsync(user);

            if (roleNames.Count > 0)
            {
                ViewBag.UserRole = roleNames[0];
            }
            else
            {
                ViewBag.UserRole = "None";
            }

            return View(user);
        }

        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            user.AllRoles        = GetAllRoles(user);
            var roleNames        = await _userManager.GetRolesAsync(user);

            if (roleNames.Count > 0)
            {
                ViewBag.UserRole = roleNames[0];
            }
            else
            {
                ViewBag.UserRole = "None";
            }

            ViewBag.IsAdmin = _userManager.IsInRoleAsync(user, "Admin").Result;

            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData)
        {
            ApplicationUser user = db.Users.Find(id);

            if (ModelState.IsValid) // Modify the user
            {
                if (id == _userManager.GetUserId(User))
                {
                    user.UserName    = newData.UserName;
                    user.Email       = newData.Email;
                    user.FirstName   = newData.FirstName;
                    user.LastName    = newData.LastName;
                    user.PhoneNumber = newData.PhoneNumber;

                    TempData["message"]     = "The user was successfully modified";
                    TempData["messageType"] = "alert-success";

                    db.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["message"]     = "You can't modify other users";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index", "Home");
                }
            }
            else // Invalid model state
            {
                TempData["message"]     = "The user coudln't be modified";
                TempData["messageType"] = "alert-danger";

                return View(newData);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(string id)
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

            db.ApplicationUsers.Remove(user);

            db.SaveChanges();

            replaceOrganizerAndManager(id);

            TempData["message"]     = "The user was successfully deleted";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestrictAccess(string id)
        {
            var user = await db.Users.FindAsync(id);

            // Don't let the admin restrict himself
            if (id == _userManager.GetUserId(User))
            {
                TempData["message"]     = "The user couldn't be restricted";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var roles = db.Roles.ToList();

            foreach (var role in roles)
            {
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }

            await replaceOrganizerAndManager(id);

            TempData["message"]     = "The user was successfully restricted";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> EditRole(string id, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);
            var roles            = db.Roles.ToList();

            foreach (var role in roles)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name);
            }

            var roleName = await _roleManager.FindByIdAsync(newRole);
            await _userManager.AddToRoleAsync(user, roleName.ToString());

            TempData["message"]     = "The user's role was successfully modified";
            TempData["messageType"] = "alert-success";

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        private async Task replaceOrganizerAndManager(string id)
        {
            // Check if the user is a manager for any team
            var isManager = db.Teams.Any(t => t.ManagerId == id);

            if (isManager)
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
            }

            // Check if the user is an organizer for any project
            var isOrganizer = db.Projects.Any(p => p.OrganizerId == id);

            if (isOrganizer)
            {
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

            await db.SaveChangesAsync();

            RedirectToAction("Index");
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllRoles(ApplicationUser? user)
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                var hasRole = _userManager.IsInRoleAsync(user, role.Name).Result;

                if (user == null || !hasRole)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = role.Id.ToString(),
                        Text = role.Name.ToString()
                    });
                }
            }

            return selectList;
        }
    }
}
