using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(string teamFilter = "ManagedTeams")
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            if (teamFilter == "AllTeams") // Select all the teams
            {
                var teams = from team in db.Teams
                            select team;
                ViewBag.Teams = teams;
                ViewBag.Count = teams.Count();
            }
            else if (teamFilter == "OldTeams") // Select the teams that the user was in
            {
                var teams = from team in db.Teams
                            where !(from teamHistory in db.TeamsHistory
                                    where teamHistory.EndDate == null
                                    select teamHistory.TeamId)
                                    .Contains(team.Id)
                            select team;
                ViewBag.Teams = teams;
                ViewBag.Count = teams.Count();
            }
            else if (teamFilter == "MyTeams") // Select the teams that the user is in
            {
                var teams = from team in db.Teams
                            join userTeam in db.UserTeams
                            on team.Id equals userTeam.TeamId
                            where userTeam.UserId == _userManager.GetUserId(User)
                            select team;
                ViewBag.Teams = teams;
                ViewBag.Count = teams.Count();
            }
            else if (teamFilter == "ManagedTeams") // Select the teams for which the user is the manager
            {
                var teams = from team in db.Teams
                            where team.ManagerId == _userManager.GetUserId(User)
                            select team;
                ViewBag.Teams = teams;
                ViewBag.Count = teams.Count();
            }
            else // Invalid team filter
            {
                TempData["message"] = "Invalid team filter";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // If the user is an admin, show all teams
            if (User.IsInRole("Admin") && teamFilter != "OldTeams")
            {
                var teams = from team in db.Teams
                            select team;
                ViewBag.Teams = teams;
                ViewBag.Count = teams.Count();
            }

            ViewBag.TeamFilter = teamFilter;

            return View();
        }

        public IActionResult Show(int id)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            Team team = db.Teams
                          .Where(t => t.Id == id)
                          .First();

            // Select the users who are in the team
            ViewBag.InTeam = from userTeam in db.UserTeams
                             join user in db.Users
                             on userTeam.UserId equals user.Id
                             where userTeam.TeamId == id
                             select user;

            // Select the users who aren't in the team
            ViewBag.NotInTeam = from user in db.Users
                                where !(from userTeam in db.UserTeams
                                        where userTeam.TeamId == id
                                        select userTeam.UserId)
                                        .Contains(user.Id)
                                select user;

            // The team's manager
            ViewBag.Manager = db.Users
                                .Where(u => u.Id == team.ManagerId)
                                .First();

            // The team's other members
            ViewBag.Members = from userTeam in db.UserTeams
                              join user in db.Users
                              on userTeam.UserId equals user.Id
                              where userTeam.TeamId == id && userTeam.UserId != team.ManagerId
                              select user;

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
                    teamHistory.TeamId = userTeam.TeamId;
                    teamHistory.UserId = userTeam.UserId;
                    teamHistory.StartDate = System.DateTime.Now;
                    db.TeamsHistory.Add(teamHistory);

                    db.UserTeams.Add(userTeam);
                    db.SaveChanges();
                    TempData["message"] = "The user was successfully added to the team";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"] = "The user is already in the team";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"] = "The user couldn't be added to the team";
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
                    teamHistory.EndDate = System.DateTime.Now;

                    db.UserTeams.Remove(userTeam);
                    db.SaveChanges();

                    TempData["message"] = "The user was successfully removed from the team";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"] = "The user is not in the team";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"] = "The user couldn't be removed from the team";
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
                db.Teams.Add(team);
                db.SaveChanges();

                // Add the manager to the team
                UserTeam userTeam = new UserTeam();
                userTeam.UserId = user.Id;
                userTeam.TeamId = team.Id;
                db.UserTeams.Add(userTeam);

                // Add the manager to the team's history
                TeamHistory teamHistory = new TeamHistory();
                teamHistory.TeamId = userTeam.TeamId;
                teamHistory.UserId = userTeam.UserId;
                teamHistory.StartDate = System.DateTime.Now;
                db.TeamsHistory.Add(teamHistory);

                db.SaveChanges();

                TempData["message"] = "The team was successfully added";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The team couldn't be added";
                ViewBag.Alert = "alert-danger";
                return View(team);
            }
        }

        // GET
        public IActionResult Edit(int id)
        {
            Team team = db.Teams
                          .Where(t => t.Id == id)
                          .First();

            if (team.ManagerId == _userManager.GetUserId(User) || 
                User.IsInRole("Admin"))
            {
                return View(team);
            }
            else
            {
                TempData["message"] = "You don't have the rights to edit this team";
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
                    team.Name = requestTeam.Name;
                    team.Description = requestTeam.Description;
                    db.SaveChanges();
                    TempData["message"] = "The team was successfully modified";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "You don't have the rights to edit this team";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else // Invalid model state
            {
                ViewBag.Message = "Couldn't modify the team";
                ViewBag.Alert = "alert-danger";
                return View(requestTeam);
            }
        }

        public IActionResult Delete(int id)
        {
            Team team = db.Teams
                          .Where(t => t.Id == id)
                          .First();
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
                TempData["message"] = "The team was deleted";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "You don't have the rights to delete this team";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
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
