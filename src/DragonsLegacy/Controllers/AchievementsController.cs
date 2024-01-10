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
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public IActionResult Index(string achievementFilter = "All")
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var achievements = from achievement in db.Achievements
                               select achievement;

            if (achievementFilter == "NotAchieved") // Select the achievements that the current user doesn't have
            {
                achievements = from achievement in db.Achievements
                               where !(from userAchievement in db.UserAchievements
                                       where userAchievement.UserId == _userManager.GetUserId(User)
                                       select userAchievement.AchievementId)
                                       .Contains(achievement.Id)
                               select achievement;
            }
            else if (achievementFilter == "Achieved") // Select the achievements that the current user has
            {
                achievements = from achievement in db.Achievements
                               join userAchievement in db.UserAchievements
                               on achievement.Id equals userAchievement.AchievementId
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
                TempData["message"] = "Invalid achievement filter";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Search engine
            var search = "";
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                // remove the spaces
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                // search in the Name and the Description
                achievements = achievements
                              .Where(ac => ac.Name.Contains(search) || ac.Description.Contains(search));
            }

            ViewBag.Achievements = achievements;
            ViewBag.Count = achievements.Count();
            ViewBag.SearchString = search;
            ViewBag.Filter = achievementFilter;

            if (search != "")
            {
                ViewBag.FilterUrl = "search=" + search + "&achievementFilter";
            }

            else
            {
                ViewBag.FilterUrl = "achievementFilter";
            }

            return View();
        }

        public IActionResult Show(int id)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            Achievement achievement = db.Achievements
                                        .Where(a => a.Id == id)
                                        .First();

            ViewBag.IsAdmin = User.IsInRole("Admin");
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
            var sanitizer = new HtmlSanitizer();
            if (ModelState.IsValid) // Add the achievement to the database
            {
                achievement.Description = sanitizer.Sanitize(achievement.Description);
                db.Achievements.Add(achievement);
                db.SaveChanges();

                TempData["message"] = "The achievement was successfully added";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The achievement couldn't be added";
                ViewBag.Alert = "alert-danger";
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
            var sanitizer = new HtmlSanitizer();
            Achievement achievement = db.Achievements.Find(id);

            if (ModelState.IsValid) // Modify the achievement
            {
                requestAchievement.Description = sanitizer.Sanitize(requestAchievement.Description);
                achievement.Name = requestAchievement.Name;
                achievement.Description = requestAchievement.Description;
                achievement.ExperiencePoints = requestAchievement.ExperiencePoints;
                achievement.Coins = requestAchievement.Coins;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
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
            return RedirectToAction("Index");
        }
    }
}
