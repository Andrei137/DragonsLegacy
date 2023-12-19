using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Task = DragonsLegacy.Models.Task;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
                                  RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Edit(int id)
        {
            Task task = db.Tasks.Find(id);
            
            // If the current user is the organizer of the project
            if (task.Project.OrganizerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                return View(task);
            }
            else
            {
                TempData["message"] = "You don't have the rights to edit this task";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
        }

        [HttpPost]
        public IActionResult Edit(int id, Task requestTask)
        {
            Task task = db.Tasks.Find(id);

            if (ModelState.IsValid) // Modify the task
            {
                if (task.Project.OrganizerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
                {
                    task.Name = requestTask.Name;
                    task.Description = requestTask.Description;
                    task.Priority = requestTask.Priority;
                    task.Status = requestTask.Status;
                    task.Deadline = requestTask.Deadline;
                    task.Multimedia = requestTask.Multimedia;
                    task.ExperiencePoints = requestTask.ExperiencePoints;
                    task.Coins = requestTask.Coins;
                    db.SaveChanges();
                    return Redirect("Projects/Show/" + task.ProjectId);
                }
                else
                {
                    TempData["message"] = "You don't have the rights to edit this task";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Projects");
                }
            }
            else // Invalid model state
            {
                return View(requestTask);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            Task task = db.Tasks.Find(id);
            if (task.Project.OrganizerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                db.Tasks.Remove(task);
                db.SaveChanges();
                return Redirect("/Projects/Show/" + task.ProjectId);
            }
            else
            {
                TempData["message"] = "You don't have the rights to remove this task";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
        }
    }
}
