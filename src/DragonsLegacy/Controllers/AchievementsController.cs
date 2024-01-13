using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class AchievementsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AchievementsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
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

            if (TempData.ContainsKey("deletebutton"))
            {
                ViewBag.DeleteButton = true;
            }
            else
            {
                ViewBag.DeleteButton = false;
            }

            var achievements = from achievement in db.Achievements
                               select achievement;

            // Filter
            var achievementFilter = "All";

            if (Convert.ToString(HttpContext.Request.Query["achievementFilter"]) != null)
            {
                achievementFilter = Convert.ToString(HttpContext.Request.Query["achievementFilter"]).Trim();
            }

            if (achievementFilter == "NotAchieved") // Select the achievements that the current user doesn't have
            {
                achievements = from achievement in db.Achievements
                               where !(
                                            from userAchievement in db.UserAchievements
                                            where userAchievement.UserId == _userManager.GetUserId(User)
                                            select userAchievement.AchievementId
                                      ).Contains(achievement.Id)
                               select achievement;
            }
            else if (achievementFilter == "Achieved") // Select the achievements that the current user has
            {
                achievements = from achievement in db.Achievements
                               join userAchievement in db.UserAchievements on achievement.Id equals userAchievement.AchievementId
                               where userAchievement.UserId == _userManager.GetUserId(User)
                               select achievement;
            }
            else if (achievementFilter == "All") // Select all achievements
            {
                achievements = from achievement in db.Achievements
                               select achievement;
            }
            else // Invalid achievement filter
            {
                TempData["message"]     = "Invalid achievement filter";
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
                achievements = achievements
                              .Where(ac => ac.Name.Contains(search) || ac.Description.Contains(search));
            }

            int perPage           = 3;
            int totalAchievements = achievements.Count();
            var currentPage       = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset            = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }

            ViewBag.Achievements      = achievements.Skip(offset).Take(perPage);
            ViewBag.Count             = totalAchievements;
            ViewBag.AchievementFilter = achievementFilter;
            ViewBag.SearchString      = search;
            ViewBag.LastPage          = Math.Ceiling((float)totalAchievements / (float)perPage);

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Achievements/Index/?achievementFilter=" + achievementFilter + 
                                            "&search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Achievements/Index/?achievementFilter=" + achievementFilter + "&page";
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

            Achievement achievement = db.Achievements.Find(id);
            ViewBag.IsAdmin         = User.IsInRole("Admin");

            return View(achievement);
        }

        // GET
        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            Achievement achievement = new Achievement();

            return View(achievement);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult New(Achievement achievement)
        {
            if (ModelState.IsValid) // Add the achievement to the database
            {
                var sanitizer           = new HtmlSanitizer();
                achievement.Description = sanitizer.Sanitize(achievement.Description);
                TempData["message"]     = "The achievement was successfully added";
                TempData["messageType"] = "alert-success";

                db.Achievements.Add(achievement);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The achievement couldn't be added";
                ViewBag.Alert   = "alert-danger";

                return View(achievement);
            }
        }

        // GET
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            Achievement achievement = db.Achievements.Find(id);

            return View(achievement);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Achievement requestAchievement)
        {
            Achievement achievement = db.Achievements.Find(id);

            if (ModelState.IsValid) // Modify the achievement
            {
                var sanitizer                  = new HtmlSanitizer();
                requestAchievement.Description = sanitizer.Sanitize(requestAchievement.Description);
                achievement.Name               = requestAchievement.Name;
                achievement.Description        = requestAchievement.Description;
                achievement.ExperiencePoints   = requestAchievement.ExperiencePoints;
                achievement.Coins              = requestAchievement.Coins;
                TempData["message"]            = "The achievement was successfully modified";
                TempData["messageType"]        = "alert-success";

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The achievement couldn't be modified";
                ViewBag.Alert   = "alert-danger";

                return View(requestAchievement);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Achievement achievement = db.Achievements.Find(id);
            db.Achievements.Remove(achievement);
            db.SaveChanges();

            TempData["message"]     = "The achievement was deleted";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }
    }
}
