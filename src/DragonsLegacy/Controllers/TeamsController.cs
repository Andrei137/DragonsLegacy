using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TeamsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
                                  RoleManager<IdentityRole> roleManager)
        {
            db           = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert   = TempData["messageType"];
            }

            var teams = from team in db.Teams
                        select team;

            // Filter
            var teamFilter = "AllTeams";

            // If the user has any managed teams, the filter is set to "ManagedTeams"
            if (db.Teams
                  .Where(team => team.ManagerId == _userManager.GetUserId(User))
                  .Count() > 0)
            {
                teamFilter = "ManagedTeams";
            }
            // If the user has any teams, the filter is set to "MyTeams"
            else if (db.UserTeams
                       .Where(ut => ut.UserId == _userManager.GetUserId(User))
                       .Count() > 0)
            {
                teamFilter = "MyTeams";
            }

            // If the user selected a team filter, use it
            if (Convert.ToString(HttpContext.Request.Query["teamFilter"]) != null)
            {
                teamFilter = Convert.ToString(HttpContext.Request.Query["teamFilter"]).Trim();
            }

            if (teamFilter == "ManagedTeams") // Select the teams for which the user is the manager
            {
                teams = from team in db.Teams
                        where team.ManagerId == _userManager.GetUserId(User)
                        select team;
            }
            else if (teamFilter == "MyTeams") // Select the teams that the user is in
            {
                teams = from team in db.Teams
                        join userTeam in db.UserTeams on team.Id equals userTeam.TeamId
                        where userTeam.UserId == _userManager.GetUserId(User)
                        select team;
            }
            else if (teamFilter == "OldTeams") // Select the teams that the user was in
            {
                // Select the old teams from the user's history
                teams = from team in db.Teams
                        join teamHistory in db.TeamsHistory on team.Id equals teamHistory.TeamId
                        where teamHistory.UserId == _userManager.GetUserId(User)
                        select team;
                
                // Exclude the teams that the user is still in
                teams = from team in teams
                        where !(
                                    from userTeam in db.UserTeams
                                    where userTeam.UserId == _userManager.GetUserId(User)
                                    select userTeam.TeamId
                               ).Contains(team.Id)
                        select team;

                // Exclude duplicate teams
                teams = teams.Distinct();
            }
            else if (teamFilter != "AllTeams") // Invalid team filter
            {
                TempData["message"]     = "Invalid team filter";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }

            // Search engine
            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                // Remove the spaces
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                // Search in the Name and the Description
                teams = teams
                        .Where(t => t.Name.Contains(search) || t.Description.Contains(search));
            }

            int perPage     = 3;
            int totalTeams  = teams.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset      = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }

            ViewBag.IsAdmin      = User.IsInRole("Admin");
            ViewBag.Teams        = teams.Skip(offset).Take(perPage);
            ViewBag.Count        = totalTeams;
            ViewBag.TeamFilter   = teamFilter;
            ViewBag.SearchString = search;
            ViewBag.LastPage     = Math.Ceiling((float)totalTeams / (float)perPage);

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Teams/Index/?teamFilter=" + teamFilter + 
                                            "&search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Teams/Index/?teamFilter=" + teamFilter + "&page";
            }

            return View();
        }

        public IActionResult Show(int id)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert   = TempData["messageType"];
            }

            Team team = db.Teams
                          .Include("Manager")
                          .Where(t => t.Id == id)
                          .First();

            // Select the users who are in the team
            ViewBag.InTeam = GetAllUsersFromTeam(team);

            // Select the users who aren't in the team
            var notInTeam = from user in db.Users
                            where !(
                                        from userTeam in db.UserTeams
                                        where userTeam.TeamId == id
                                        select userTeam.UserId
                                   ).Contains(user.Id)
                            select user;

            // Remove the users with the role admin;
            ViewBag.NotInTeam = from user in notInTeam
                                where !(
                                            from userRole in db.UserRoles
                                            join role in db.Roles on userRole.RoleId equals role.Id
                                            where role.Name == "Admin"
                                            select userRole.UserId
                                       ).Contains(user.Id)
                                select user;

            // The team's manager
            ViewBag.Manager = db.Users.Find(team.ManagerId);

            // The team's other members
            ViewBag.Members = from userTeam in db.UserTeams
                              join user in db.Users on userTeam.UserId equals user.Id
                              where userTeam.TeamId == id && userTeam.UserId != team.ManagerId
                              select user;

            // Select all the tasks that are assigned to a member in the team
            var tasks = from task in db.Tasks
                        where (
                                   from userTeam in db.UserTeams
                                   join user in db.Users on userTeam.UserId equals user.Id
                                   where userTeam.TeamId == id
                                   select user.Id
                               ).Contains(task.UserId)
                        select task;

            var taskFilter = "AllTasks";

            if (Convert.ToString(HttpContext.Request.Query["taskFilter"]) != null)
            {
                taskFilter = Convert.ToString(HttpContext.Request.Query["taskFilter"]).Trim();
            }

            if (taskFilter == "MyTasks")
            {
                tasks = from task in tasks
                        where task.UserId ==_userManager.GetUserId(User)
                        select task;
            }
            else if (taskFilter == "OthersTasks")
            {
                tasks = from task in tasks
                        where task.UserId !=_userManager.GetUserId(User)
                        select task;
            }

            int perPage     = 2;
            int totalTasks  = tasks.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset      = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }

            ViewBag.Tasks             = tasks.Skip(offset).Take(perPage);
            ViewBag.Count             = totalTasks;
            ViewBag.TaskFilter        = taskFilter;
            ViewBag.LastPage          = Math.Ceiling((float)totalTasks / (float)perPage);
            ViewBag.PaginationBaseUrl = "/Teams/Show/" + id + "/?taskFilter=" + taskFilter + "&page";

            SetAccessRights(team);

            return View(team);
        }

        [HttpPost]
        public IActionResult AddUser([FromForm] UserTeam userTeam)
        {
            if (ModelState.IsValid)
            {
                // If the user isn't in this team, add him
                if (db.UserTeams
                      .Where(ut => ut.UserId == userTeam.UserId && ut.TeamId == userTeam.TeamId)
                      .Count() == 0)
                {
                    // Add the user to the team's history
                    TeamHistory teamHistory = new TeamHistory();
                    teamHistory.TeamId      = userTeam.TeamId;
                    teamHistory.UserId      = userTeam.UserId;
                    teamHistory.StartDate   = System.DateTime.Now;

                    db.TeamsHistory.Add(teamHistory);
                    db.UserTeams.Add(userTeam);
                    db.SaveChanges();

                    TempData["message"]     = "The user was successfully added to the team";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"]     = "The user is already in the team";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"]     = "The user couldn't be added to the team";
                TempData["messageType"] = "alert-danger";
            }

            return Redirect("/Teams/Show/" + userTeam.TeamId);
        }

        [HttpPost]
        public IActionResult RemoveUser([FromForm] UserTeam userTeam)
        {
            if (ModelState.IsValid)
            {
                // If the user is in this team, remove him
                if (db.UserTeams
                      .Where(ut => ut.UserId == userTeam.UserId && ut.TeamId == userTeam.TeamId)
                      .Count() == 1)
                {
                    // We find the most recent entry in the history
                    TeamHistory teamHistory = db.TeamsHistory
                                               .Where(th => th.TeamId == userTeam.TeamId &&
                                                            th.UserId == userTeam.UserId)
                                               .OrderByDescending(th => th.StartDate)
                                               .First();
                    teamHistory.EndDate     = System.DateTime.Now;

                    db.UserTeams.Remove(userTeam);
                    db.SaveChanges();

                    TempData["message"]     = "The user was successfully removed from the team";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"]     = "The user is not in the team";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"]     = "The user couldn't be removed from the team";
                TempData["messageType"] = "alert-danger";
            }

            return Redirect("/Teams/Show/" + userTeam.TeamId);
        }

        // GET 
        public IActionResult New()
        {
            Team team = new Team();

            return View(team);
        }

        [HttpPost]
        public IActionResult New(Team team)
        {
            ApplicationUser user = _userManager.GetUserAsync(User).Result;

            // The current user becomes the manager
            team.ManagerId = user.Id;

            if (ModelState.IsValid) // Add the team to the database
            {
                var sanitizer    = new HtmlSanitizer();
                team.Description = sanitizer.Sanitize(team.Description);

                db.Teams.Add(team);
                db.SaveChanges();

                // Add the manager to the team
                UserTeam userTeam = new UserTeam();
                userTeam.UserId   = user.Id;
                userTeam.TeamId   = team.Id;

                // Add the manager to the team's history
                TeamHistory teamHistory = new TeamHistory();
                teamHistory.TeamId      = userTeam.TeamId;
                teamHistory.UserId      = userTeam.UserId;
                teamHistory.StartDate   = System.DateTime.Now;

                db.UserTeams.Add(userTeam);
                db.TeamsHistory.Add(teamHistory);
                db.SaveChanges();

                TempData["message"]     = "The team was successfully added";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The team couldn't be added";
                ViewBag.Alert   = "alert-danger";

                return View(team);
            }
        }

        // GET
        public IActionResult Edit(int id)
        {
            Team team = db.Teams
                          .Include("Manager")
                          .Where(t => t.Id == id)
                          .First();

            if (team.ManagerId == _userManager.GetUserId(User) || 
                User.IsInRole("Admin"))
            {
                 ViewBag.InTeam = GetAllUsersFromTeam(team);

                return View(team);
            }
            else
            {
                TempData["message"]     = "You don't have the rights to modify this team";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Edit(int id, Team requestTeam)
        {
            Team team = db.Teams.Find(id);

            if (ModelState.IsValid) // Modify the team
            {
                if (team.ManagerId == _userManager.GetUserId(User) ||
                    User.IsInRole("Admin"))
                {
                    var sanitizer           = new HtmlSanitizer();
                    requestTeam.Description = sanitizer.Sanitize(requestTeam.Description);
                    team.Name               = requestTeam.Name;
                    team.Description        = requestTeam.Description;
                    team.ManagerId          = requestTeam.ManagerId;
                    TempData["message"]     = "The team was successfully modified";
                    TempData["messageType"] = "alert-success";

                    db.SaveChanges();
                    
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"]     = "You don't have the rights to modify this team";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index");
                }
            }
            else // Invalid model state
            {
                ViewBag.Message = "Couldn't modify the team";
                ViewBag.Alert   = "alert-danger";

                return View(requestTeam);
            }
        }

        public IActionResult Delete(int id)
        {
            Team team = db.Teams.Find(id);

            if (team.ManagerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                // Remove the team's history
                var teamHistory = db.TeamsHistory
                                  .Where(th => th.TeamId == id);

                foreach (var history in teamHistory)
                {
                    db.TeamsHistory.Remove(history);
                }

                db.Teams.Remove(team);
                db.SaveChanges();

                TempData["message"]     = "The team was deleted";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"]     = "You don't have the rights to delete this team";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllUsersFromTeam(Team team)
        {
            var selectList = new List<SelectListItem>();

            foreach (var user in db.Users.ToList())
            {
                // Select all users from the team which aren't the manager
                if (db.UserTeams
                      .Where(ut => ut.UserId == user.Id && ut.TeamId == team.Id)
                      .Count() != 0)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value    = user.Id.ToString(),
                        Text     = user.UserName,
                        Selected = team.ManagerId == user.Id
                    });
                }
            }

            return selectList;
        }

        private void SetAccessRights(Team team)
        {
            ViewBag.ShowButtons = false;

            if (team.ManagerId == _userManager.GetUserId(User))
            {
                ViewBag.ShowButtons = true;
            }

            ViewBag.IsAdmin = User.IsInRole("Admin");
        }
    }
}
