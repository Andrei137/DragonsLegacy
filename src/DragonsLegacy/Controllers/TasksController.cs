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

        public IActionResult Index(string taskFilter = "NotStarted")
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            if (taskFilter == "NotStarted" || taskFilter == "InProgress" || taskFilter == "Completed")
            { // Select the tasks that have the status NotStarted, InProgress or Completed
              
                // Current user
                var userId = _userManager.GetUserId(User);

                // Select the tasks of the current user
                var tasks = from task in db.Tasks
                            where task.UserId == userId && task.Status == taskFilter
                            select task;

                ViewBag.Tasks = tasks;
                ViewBag.Count = tasks.Count();
            }
            else // Invalid task filter
            {
                TempData["message"] = "Invalid task filter";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
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

            Task task = db.Tasks
                          .Where(p => p.Id == id)
                          .First();

            SetAccessRights(task);
            return View(task);
        }

        public IActionResult Edit(int id)
        {
            Task task = db.Tasks.Find(id);

            SetAccessRights(task);

            // If the current user has the rights to edit
            if (ViewBag.ShowButtons)
            {
                return View(task);
            }
            else
            {
                TempData["message"] = "You don't have the rights to edit this task";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Edit(int id, Task requestTask)
        {
            Task task = db.Tasks.Find(id);
            SetAccessRights(task);

            if (ModelState.IsValid) // Modify the task
            {
                if (ViewBag.IsOrganizer)
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
                    return RedirectToAction("Index");
                }
                else if (ViewBag.IsUser)
                {
                    task.Status = requestTask.Status;
                    if(task.Status == "Completed")
                    {
                        task.EndDate = DateTime.Now;
                    }
                    else
                    {
                        task.EndDate = null;
                    }
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "You don't have the rights to edit this task";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
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
            SetAccessRights(task);

            if (ViewBag.IsOrganizer || ViewBag.IsAdmin)
            {
                db.Tasks.Remove(task);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "You don't have the rights to remove this task";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }
        private void SetAccessRights(Task task)
        {
            ViewBag.ShowButtons = false;
            ViewBag.IsOrganizer = false;
            ViewBag.IsUser = false;

            var projects = from project in db.Projects
                                  where project.Id == task.ProjectId
                                  select project;
            var current_project = projects.First();

            // If the current user is the organizer
            if (current_project.OrganizerId == _userManager.GetUserId(User))
            {
                ViewBag.IsOrganizer = true;
            }

            // If this is the task of the current user, show the buttons
            if (task.UserId == _userManager.GetUserId(User))
            {
                ViewBag.IsUser = true;
            }

            // If the current user is the organizer or this is his task, show the buttons
            if (current_project.OrganizerId == _userManager.GetUserId(User) || task.UserId == _userManager.GetUserId(User))
            {
                ViewBag.ShowButtons = true;
            }

            ViewBag.IsAdmin = User.IsInRole("Admin");
        }
    }
}
