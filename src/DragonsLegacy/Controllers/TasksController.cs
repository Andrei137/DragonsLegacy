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
            db           = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env         = env;
        }

        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var tasks = from task in db.Tasks
                        select task;
            
            // Filter
            var taskFilter = "AllTasks";

            // Search engine
            var search = "";

            int perPage = 3;
            int totalTasks = tasks.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = 0;

            // If the current user is admin, show all tasks
            if (User.IsInRole("Admin"))
            {
                if (Convert.ToString(HttpContext.Request.Query["taskFilter"]) != null)
                {
                    taskFilter = Convert.ToString(HttpContext.Request.Query["taskFilter"]).Trim();
                }

                if (taskFilter != "AllTasks")
                {
                    tasks = from task in db.Tasks
                            where task.UserId == taskFilter
                            select task;
                }

                if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
                {
                    // Remove the spaces
                    search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                    // Search in the Name and the Description
                    tasks = tasks
                            .Where(t => t.Name.Contains(search) || t.Description.Contains(search));
                }

                totalTasks = tasks.Count();

                if (!currentPage.Equals(0))
                {
                    offset = (currentPage - 1) * perPage;
                }

                ViewBag.Tasks           = tasks.Skip(offset).Take(perPage);
                ViewBag.Count           = totalTasks;
                ViewBag.AllUsers        = GetAllUsers();
                ViewBag.IsAdmin         = true;
                ViewBag.TaskFilter      = taskFilter;
                ViewBag.TaskFilterValue = "All Tasks";
                ViewBag.TaskFilter      = taskFilter;
                ViewBag.SearchString    = search;
                ViewBag.LastPage        = Math.Ceiling((float)totalTasks / (float)perPage);

                if (search != "")
                {
                    ViewBag.PaginationBaseUrl = "/Tasks/Index/?taskFilter=" + taskFilter + 
                                                "&search=" + search + "&page";
                }
                else
                {
                    ViewBag.PaginationBaseUrl = "/Tasks/Index/?taskFilter=" + taskFilter + "&page";
                }

                if (taskFilter != "AllTasks")
                {
                    ViewBag.TaskFilterValue = db.Users.Find(taskFilter).UserName;
                }

                return View();
            }

            if (Convert.ToString(HttpContext.Request.Query["taskFilter"]) != null)
            {
                taskFilter = Convert.ToString(HttpContext.Request.Query["taskFilter"]).Trim();
            }

            if (taskFilter == "AllTasks")
            {
                var userId = _userManager.GetUserId(User);

                // Select the tasks of the current user
                tasks = from task in db.Tasks
                        where task.UserId == userId
                        select task;
            }
            // Select the tasks that have the status NotStarted, InProgress or Completed
            else if (taskFilter == "NotStarted" || taskFilter == "InProgress" || taskFilter == "Completed")
            {
              
                // Current user
                var userId = _userManager.GetUserId(User);

                // Select the tasks of the current user
                tasks = from task in db.Tasks
                        where task.UserId == userId && task.Status == taskFilter
                        select task;
            }
            else // Invalid task filter
            {
                TempData["message"]     = "Invalid task filter";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                // Remove the spaces
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                // Search in the Name and the Description
                tasks = tasks
                        .Where(t => t.Name.Contains(search) || t.Description.Contains(search));
            }

            totalTasks = tasks.Count();

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }

            ViewBag.Tasks        = tasks.Skip(offset).Take(perPage);
            ViewBag.Count        = totalTasks;
            ViewBag.TaskFilter   = taskFilter;
            ViewBag.SearchString = search;
            ViewBag.LastPage      = Math.Ceiling((float)totalTasks / (float)perPage);
            ViewBag.IsAdmin      = false;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Tasks/Index/?taskFilter=" + taskFilter + 
                                            "&search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Tasks/Index/?taskFilter=" + taskFilter + "&page";
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

            Task task = db.Tasks
                          .Include("Comments")
                          .Include("Comments.User")
                          .Where(t => t.Id == id)
                          .First();

            ViewBag.UserId = _userManager.GetUserId(User);

            SetAccessRights(task);

            return View(task);
        }

        [HttpPost]
        public IActionResult EditStatus([FromForm] Task requestTask)
        {
            Task task               = db.Tasks.Find(requestTask.Id);
            var sanitizer           = new HtmlSanitizer();
            requestTask.Description = sanitizer.Sanitize(requestTask.Description);
            task.Name               = requestTask.Name;
            task.Description        = requestTask.Description;
            task.Priority           = requestTask.Priority;
            task.Status             = requestTask.Status;
            task.Deadline           = requestTask.Deadline;
            task.Multimedia         = requestTask.Multimedia;
            task.ExperiencePoints   = requestTask.ExperiencePoints;
            task.Coins              = requestTask.Coins;
            task.Status             = requestTask.Status;

            if (task.Status == "Completed")
            {
                // If the task is completed, remove the rewards
                // So the completed status won't be exploited
                task.ExperiencePoints = 0;
                task.Coins            = 0;
                task.EndDate          = DateTime.Now;
            }
            else
            {
                task.EndDate = null;
            }

            db.SaveChanges();

            SetAccessRights(task);

            return Redirect("/Tasks/Index/?taskFilter=" + task.Status);
        }


        [HttpPost]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
                var sanitizer           = new HtmlSanitizer();
                comment.Content         = sanitizer.Sanitize(comment.Content);
                TempData["message"]     = "The comment was successfully added";
                TempData["messageType"] = "alert-success";
                
                db.Comments.Add(comment);
                db.SaveChanges();

                return Redirect("/Tasks/Show/" + comment.TaskId);
            }
            else
            {
                TempData["message"]     = "The comment couldn't be added";
                TempData["messageType"] = "alert-danger";

                return Redirect("/Tasks/Show/" + comment.TaskId);
            }
        }

        public IActionResult New()
        {
            int ProjectId         = (int)TempData["ProjectId"];
            Task task             = new Task();
            task.Deadline         = task.StartDate = DateTime.Now;
            task.ExperiencePoints = task.Coins = 0;

            if (ProjectId == null)
            {
                TempData["message"]     = "The project does not exist.";
                TempData["messageType"] = "alert-danger";

                return Redirect("/Projects/Index");
            }
            else
            {
                Project project = db.Projects.Find(ProjectId);

                if (project.OrganizerId != _userManager.GetUserId(User))
                {
                    TempData["message"]     = "You don't have the rights to add a task.";
                    TempData["messageType"] = "alert-danger";

                    return Redirect("/Projects/Index");
                }
                else
                {
                    ViewBag.ProjectId     = ProjectId;
                    ViewBag.AllUsers      = GetAllUsers(project);
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
                        var storagePath      = Path.Combine(_env.WebRootPath, "files", Media.FileName);
                        var databaseFileName = "/files/" + Media.FileName;

                        using (var fileStream = new FileStream(storagePath, FileMode.Create))
                        {
                            await Media.CopyToAsync(fileStream);
                        }

                        task.Multimedia = databaseFileName;
                    }

                    var sanitizer    = new HtmlSanitizer();
                    task.Description = sanitizer.Sanitize(task.Description);

                    db.Tasks.Add(task);
                    db.SaveChanges();

                    if (task.SelectedCategories != null)
                    {
                        foreach (var category in task.SelectedCategories)
                        {
                            TaskCategory taskCategory = new TaskCategory();
                            taskCategory.TaskId       = task.Id;
                            taskCategory.CategoryId   = category; // already the id

                            db.TaskCategories.Add(taskCategory);
                        }
                        db.SaveChanges();
                    }

                    TempData["message"]     = "The task was successfully added";
                    TempData["messageType"] = "alert-success";

                    return Redirect("/Projects/Show/" + @task.ProjectId);
                }
                else
                {
                    TempData["message"]     = "You don't have the rights to add a task.";
                    TempData["messageType"] = "alert-danger";

                    return Redirect("/Projects/Index");
                }
            }
            else // Invalid model state
            {
                Project project            = db.Projects.Find(task.ProjectId);
                ViewBag.ProjectId          = task.ProjectId;
                ViewBag.AllUsers           = GetAllUsers(project);
                ViewBag.AllCategories      = GetAllCategories();
                ViewBag.SelectedCategories = task.SelectedCategories;
                ViewBag.Message            = "The task couldn't be added";
                ViewBag.Alert              = "alert-danger";

                return View(task);
            }
        }

        public IActionResult Edit(int id)
        {
            Task task = db.Tasks.Find(id);

            SetAccessRights(task);

            // If the current user has the rights to edit
            if (ViewBag.IsOrganizer || ViewBag.IsAdmin)
            {
                IEnumerable<SelectListItem> users = GetAllUsers(task.Project);
                ViewBag.AllUsers                  = users.Where(u => u.Value != task.UserId);
                ViewBag.AllCategories             = GetAllCategories();
                ViewBag.SelectedCategories        = from taskCategory in db.TaskCategories
                                                    where taskCategory.TaskId == id
                                                    select taskCategory.CategoryId;

                return View(task);
            }
            else
            {
                TempData["message"]     = "You don't have the rights to edit this task";
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
                    // Remove the old categories from the task
                    var taskCategories = from taskCategory in db.TaskCategories
                                         where taskCategory.TaskId == id
                                         select taskCategory;
                        
                    foreach (var taskCategory in taskCategories)
                    {
                        db.TaskCategories.Remove(taskCategory);
                    }

                    // Add the new categories to the task
                    if (requestTask.SelectedCategories != null)
                    {
                        foreach (var category in requestTask.SelectedCategories)
                        {
                            TaskCategory taskCategory = new TaskCategory();
                            taskCategory.TaskId       = id;
                            taskCategory.CategoryId   = category; // already the id

                            db.TaskCategories.Add(taskCategory);
                        }
                    }

                    var sanitizer           = new HtmlSanitizer();
                    requestTask.Description = sanitizer.Sanitize(requestTask.Description);
                    task.Name               = requestTask.Name;
                    task.Description        = requestTask.Description;
                    task.Priority           = requestTask.Priority;
                    task.Status             = requestTask.Status;
                    task.Deadline           = requestTask.Deadline;
                    task.Multimedia         = requestTask.Multimedia;
                    task.ExperiencePoints   = requestTask.ExperiencePoints;
                    task.Coins              = requestTask.Coins;
                    
                    db.SaveChanges();
                    
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"]     = "You don't have the rights to edit this task";
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
                TempData["message"]     = "You don't have the rights to remove this task";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }
        private void SetAccessRights(Task task)
        {
            ViewBag.ShowButtons = false;
            ViewBag.IsOrganizer = false;
            ViewBag.IsUser      = false;
            ViewBag.CurrentUser = _userManager.GetUserId(User);
            var projects        = from project in db.Projects
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
                        join teamProject in db.TeamProjects on userTeam.TeamId equals teamProject.TeamId
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
                        Text  = user.UserName.ToString()
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
                    Value    = category.Id.ToString(),
                    Text     = category.Name.ToString()
                });
            }
            return selectList;
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllUsers()
        {
            var selectList = new List<SelectListItem>();
            var users      = from user in db.Users
                             select user;

            foreach (var user in users)
            {
                bool isAdmin = false;

                var role = _userManager.GetRolesAsync(user);
                if (role.Result.First() == "Admin")
                {
                    isAdmin = true;
                }

                if (!isAdmin)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = user.Id.ToString(),
                        Text  = user.UserName.ToString()
                    });
                }
            }
            return selectList;
        }
    }
}
