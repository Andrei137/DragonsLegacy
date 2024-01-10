using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Task = DragonsLegacy.Models.Task;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IWebHostEnvironment _env;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
                                  RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
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
                          .Include("Comments")
                          .Include("Comments.User")
                          .Where(t => t.Id == id)
                          .First();

            SetAccessRights(task);
            return View(task);
        }

        [HttpPost]
        public IActionResult Show([FromForm] Comment comment)
        {
            ModelState.Clear();
            var sanitizer = new HtmlSanitizer();

            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                TempData["message"] = "Success";
                TempData["messageType"] = "alert-success";
                comment.Content = sanitizer.Sanitize(comment.Content);
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Tasks/Show/" + comment.TaskId);
            }
            else
            {
                TempData["message"] = "Error";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        public IActionResult New()
        {
            int ProjectId = (int)TempData["ProjectId"];
            Task task = new Task();
            task.Deadline = task.StartDate = DateTime.Now;
            task.ExperiencePoints = task.Coins = 0;

            if (ProjectId == null)
            {
                TempData["message"] = "The project does not exist.";
                TempData["messageType"] = "alert-danger";

                return Redirect("/Projects/Index");
            }
            else
            {
                Project project = db.Projects.Find(ProjectId);
                if (project.OrganizerId != _userManager.GetUserId(User))
                {
                    TempData["message"] = "You don't have the rights to add a task.";
                    TempData["messageType"] = "alert-danger";

                    return Redirect("/Projects/Index");
                }
                else
                {
                    ViewBag.ProjectId = ProjectId;
                    ViewBag.AllUsers = GetAllUsers(project);
                    ViewBag.AllCategories = GetAllCategories();
                    return View(task);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> New(Task task, IFormFile Media)
        {
            if (ModelState.IsValid) // Add the task to the database
            {
                Project project = db.Projects.Find(task.ProjectId);
                if (project.OrganizerId == _userManager.GetUserId(User))
                {

                    if (Media.Length > 0)
                    {
                        var storagePath = Path.Combine(_env.WebRootPath, "files", Media.FileName);
                        var databaseFileName = "/files/" + Media.FileName;

                        using (var fileStream = new FileStream(storagePath, FileMode.Create))
                        {
                            await Media.CopyToAsync(fileStream);
                        }

                        task.Multimedia = databaseFileName;
                    }

                    var sanitizer = new HtmlSanitizer();
                    task.Description = sanitizer.Sanitize(task.Description);
                    db.Tasks.Add(task);
                    db.SaveChanges();

                    if (task.SelectedCategories != null)
                    {
                        foreach (var category in task.SelectedCategories)
                        {
                            TaskCategory taskCategory = new TaskCategory();
                            taskCategory.TaskId = task.Id;
                            taskCategory.CategoryId = category; // already the id
                            db.TaskCategories.Add(taskCategory);
                        }
                        db.SaveChanges();
                    }

                    TempData["message"] = "Task " + task.Name + " was added";
                    TempData["messageType"] = "alert-success";

                    return Redirect("/Projects/Show/" + task.ProjectId);
                }

                else
                {
                    TempData["message"] = "You don't have the rights to add a task.";
                    TempData["messageType"] = "alert-danger";

                    return Redirect("/Projects/Index");
                }
            }
            else // Invalid model state
            {
                Console.WriteLine("\n");
                Console.WriteLine("\n");
                Console.WriteLine(task.Id);
                Console.WriteLine(task.Name);
                Console.WriteLine(task.Description);
                Console.WriteLine(task.Priority);
                Console.WriteLine(task.Status);
                Console.WriteLine(task.Deadline);
                Console.WriteLine(task.Multimedia);
                Console.WriteLine(task.ExperiencePoints);
                Console.WriteLine(task.Coins);
                Console.WriteLine(task.ProjectId);
                Console.WriteLine(task.UserId);
                Console.WriteLine("\n");
                Console.WriteLine("\n");

                Project project = db.Projects.Find(task.ProjectId);

                ViewBag.Message = "The task couldn't be added";
                ViewBag.Alert = "alert-danger";
                ViewBag.ProjectId = task.ProjectId;
                ViewBag.AllUsers = GetAllUsers(project);
                ViewBag.AllCategories = GetAllCategories();
                ViewBag.SelectedCategories = task.SelectedCategories;

                return View(task);
            }
        }

        public IActionResult Edit(int id)
        {
            Task task = db.Tasks.Find(id);

            SetAccessRights(task);

            // If the current user has the rights to edit
            if (ViewBag.ShowButtons)
            {
                IEnumerable<SelectListItem> users = GetAllUsers(task.Project);
                users = users.Where(u => u.Value != task.UserId);

                ViewBag.AllUsers = users;
                ViewBag.AllCategories = GetAllCategories();

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
            var sanitizer = new HtmlSanitizer();
            Task task = db.Tasks.Find(id);
            SetAccessRights(task);

            if (ModelState.IsValid) // Modify the task
            {
                if (ViewBag.IsOrganizer)
                {
                    // remove the old categories from the task
                    var taskCategories = from taskCategory in db.TaskCategories
                                         where taskCategory.TaskId == id
                                         select taskCategory;
                        
                    foreach (var taskCategory in taskCategories)
                    {
                        db.TaskCategories.Remove(taskCategory);
                    }

                    // add the new categories to the task
                    if (requestTask.SelectedCategories != null)
                    {
                        foreach (var category in requestTask.SelectedCategories)
                        {
                            TaskCategory taskCategory = new TaskCategory();
                            taskCategory.TaskId = id;
                            taskCategory.CategoryId = category; // already the id
                            db.TaskCategories.Add(taskCategory);
                        }
                    }

                    requestTask.Description = sanitizer.Sanitize(requestTask.Description);
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
            Task task = db.Tasks
                          .Include("Comments")
                          .Where(t => t.Id == id)
                          .First();

            SetAccessRights(task);

            if (ViewBag.IsOrganizer || ViewBag.IsAdmin)
            {
                // Delete associated comments
                if (task.Comments.Count > 0)
                {
                    foreach (var comment in task.Comments)
                    {
                        db.Comments.Remove(comment);
                    }
                }

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
            ViewBag.CurrentUser = _userManager.GetUserId(User);

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

        [NonAction]
        private IEnumerable<SelectListItem> GetAllUsers(Project project)
        {
            // Select all user objects from all teams working on the project
            var users = from userTeam in db.UserTeams
                        join teamProject in db.TeamProjects
                        on userTeam.TeamId equals teamProject.TeamId
                        where teamProject.ProjectId == project.Id
                        select userTeam.User;

            var selectList = new List<SelectListItem>();
            foreach (var user in users)
            {
                // add the user if he isn't admin
                var role = _userManager.GetRolesAsync(user);

                if (role.Result.First() != "Admin")
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = user.Id.ToString(),
                        Text = user.UserName.ToString()
                    });
                }
            }

            return selectList;
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllCategories()
        {
            var selectList = new List<SelectListItem>();
            var categories = from category in db.Categories
                             select category;

            foreach (var category in categories)
            {
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.Name.ToString(),
                    Selected = (from taskCategory in db.TaskCategories
                                where taskCategory.TaskId == category.Id
                                select taskCategory).Any()
                });
            }
            return selectList;
        }
    }
}
