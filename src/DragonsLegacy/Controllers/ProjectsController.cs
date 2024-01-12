﻿using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Task = DragonsLegacy.Models.Task;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
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
   
            var projects = from project in db.Projects
                           select project;
                            
            // Filter
            var projectFilter = "AllProjects";

            // If the user has any organized projects, the filter is set to OrganizedProjects
            if (db.Projects
                  .Where(p => p.OrganizerId == _userManager.GetUserId(User))
                  .Count() > 0)
            {
                projectFilter = "OrganizedProjects";
            }
            // Else if the user has any projects, the filter is set to MyProjects
            else if (db.UserProjects
                       .Where(up => up.UserId == _userManager.GetUserId(User))
                       .Count() > 0)
            {
                projectFilter = "MyProjects";
            }

            // If the user selected a filter, use it
            if (Convert.ToString(HttpContext.Request.Query["projectFilter"]) != null)
            {
                projectFilter = Convert.ToString(HttpContext.Request.Query["projectFilter"]).Trim();
            }

            if (projectFilter == "OrganizedProjects") // Select the projects for which the user is the organizer
            {
                projects = from project in db.Projects
                           where project.OrganizerId == _userManager.GetUserId(User)
                           select project;
            }
            else if (projectFilter == "MyProjects") // Select the projects the user is working on
            {
                projects = from project in db.Projects
                           join userProject in db.UserProjects on project.Id equals userProject.ProjectId
                           where userProject.UserId == _userManager.GetUserId(User)
                           select project;
            }
            else if (projectFilter != "AllProjects") // Invalid project filter
            {
                TempData["message"] = "Invalid project filter";
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
                projects = projects
                           .Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            int perPage = 3;
            int totalProjects = projects.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }

            ViewBag.Projects      = projects.Skip(offset).Take(perPage);
            ViewBag.Count         = totalProjects;
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.SearchString  = search;
            ViewBag.LastPage      = Math.Ceiling((float)totalProjects / (float)perPage);

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Projects/Index/?projectFilter=" + projectFilter + 
                                            "&search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Projects/Index/?projectFilter=" + projectFilter + "&page";
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

            Project project = db.Projects
                                .Where(p => p.Id == id)
                                .First();

            // Select the users who are in the project
            ViewBag.InProject = from teamProject in db.TeamProjects
                                join team in db.Teams on teamProject.TeamId equals team.Id
                                where teamProject.ProjectId == id
                                select team;

            // Select the users who aren't in the project
            ViewBag.NotInProject = from team in db.Teams
                                   where !(
                                                from teamProject in db.TeamProjects
                                                where teamProject.ProjectId == id
                                                select teamProject.TeamId
                                          ).Contains(team.Id)
                                   select team;

            // The project's teams
            ViewBag.Teams = from teamProject in db.TeamProjects
                            join team in db.Teams on teamProject.TeamId equals team.Id
                            where teamProject.ProjectId == id
                            select team;

            // The project's tasks
            ViewBag.Tasks = from task in db.Tasks
                            where task.ProjectId == id
                            select task;

            // Every user in the project
            ViewBag.AllUsers = GetAllUsersFromProject(project);

            SetAccessRights(project);

            return View(project);
        }

        [HttpPost]
        public IActionResult AddTeam([FromForm] TeamProject teamProject)
        {
            if (ModelState.IsValid)
            {
                // If the team isn't in the project, add it
                if (db.TeamProjects
                      .Where(tp => tp.TeamId == teamProject.TeamId && tp.ProjectId == teamProject.ProjectId)
                      .Count() == 0)
                {
                    db.TeamProjects.Add(teamProject);
                    db.SaveChanges();
                        
                    // If the organizer isn't in the added team, add him
                    if (db.UserTeams
                          .Where(ut => ut.TeamId == teamProject.TeamId && ut.UserId == _userManager.GetUserId(User))
                          .Count() == 0)
                    {
                        UserTeam userTeam = new UserTeam();
                        userTeam.TeamId   = teamProject.TeamId;
                        userTeam.UserId   = _userManager.GetUserId(User);

                        db.UserTeams.Add(userTeam);
                        db.SaveChanges();
                    }

                    // Every user in the team is added to the project
                    var users = from userTeam in db.UserTeams
                                where userTeam.TeamId == teamProject.TeamId
                                select userTeam.UserId;

                    foreach (string userId in users)
                    {
                        // If the user isn't in the project, add him
                        if (db.UserProjects
                              .Where(up => up.ProjectId == teamProject.ProjectId && up.UserId == userId)
                              .Count() == 0)
                        {
                            UserProject userProject = new UserProject();
                            userProject.ProjectId   = teamProject.ProjectId;
                            userProject.UserId      = userId;

                            db.UserProjects.Add(userProject);
                        }
                    }
                    db.SaveChanges();

                    TempData["message"]     = "The team was successfully added to the project";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"]     = "The team is already in the project";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"]     = "The team couldn't be added to the project";
                TempData["messageType"] = "alert-danger";
            }

            return Redirect("/Projects/Show/" + teamProject.ProjectId);
        }

        [HttpPost]
        public IActionResult RemoveTeam([FromForm] TeamProject teamProject)
        {
            if (ModelState.IsValid)
            {
                // If the team is in the project, remove it
                if (db.TeamProjects
                      .Where(tp => tp.TeamId == teamProject.TeamId && tp.ProjectId == teamProject.ProjectId)
                      .Count() == 1)
                {
                    db.TeamProjects.Remove(teamProject);
                    db.SaveChanges();

                    // Every user in the team is removed from the project
                    var users = from userTeam in db.UserTeams
                                where userTeam.TeamId == teamProject.TeamId
                                select userTeam.UserId;

                    foreach (string userId in users)
                    {
                        // If the user is in another team working on the project, don't remove him
                        if (db.TeamProjects
                              .Where(tp => tp.ProjectId == teamProject.ProjectId && tp.TeamId != teamProject.TeamId)
                              .Count() == 0)
                        {
                            UserProject userProject = db.UserProjects
                                                      .Where(up => up.ProjectId == teamProject.ProjectId && up.UserId == userId)
                                                      .First();
                            db.UserProjects.Remove(userProject);
                        }
                    }
                    db.SaveChanges();

                    TempData["message"]     = "The team was successfully removed from the project";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"]     = "The team is not in the project";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                TempData["message"]     = "The team couldn't be removed from the project";
                TempData["messageType"] = "alert-danger";
            }

            return Redirect("/Projects/Show/" + teamProject.ProjectId);
        }

        // GET 
        public IActionResult New()
        {
            Project project = new Project();

            return View(project);
        }

        [HttpPost]
        public IActionResult New(Project project)
        {
            ApplicationUser user = _userManager.GetUserAsync(User).Result;

            // The current user becomes the organizer
            project.OrganizerId = user.Id;

            if (ModelState.IsValid) // Add the project to the database
            {
                var sanitizer       = new HtmlSanitizer();
                project.Description = sanitizer.Sanitize(project.Description);

                db.Projects.Add(project);
                db.SaveChanges();

                // Add the manager to the team
                UserProject userProject = new UserProject();
                userProject.UserId      = user.Id;
                userProject.ProjectId   = project.Id;

                db.UserProjects.Add(userProject);
                db.SaveChanges();

                TempData["message"]     = "The project was successfully added";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The project couldn't be added";
                ViewBag.Alert   = "alert-danger";

                return View(project);
            }
        }

        // GET
        public IActionResult Edit(int id)
        {
            Project project = db.Projects
                                .Where(p => p.Id == id)
                                .First();

            if (project.OrganizerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                ViewBag.InProject = GetAllUsersFromProject(project);

                return View(project);
            }
            else
            {
                TempData["message"]     = "You don't have the rights to modify this project";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Edit(int id, Project requestProject)
        {
            Project project = db.Projects.Find(id);

            if (ModelState.IsValid) // Modify the project
            {
                if (project.OrganizerId == _userManager.GetUserId(User) ||
                    User.IsInRole("Admin"))
                {
                    var sanitizer              = new HtmlSanitizer();
                    requestProject.Description = sanitizer.Sanitize(requestProject.Description);
                    project.Name               = requestProject.Name;
                    project.Description        = requestProject.Description;
                    project.OrganizerId        = requestProject.OrganizerId;
                    TempData["message"]        = "The project was successfully modified";
                    TempData["messageType"]    = "alert-success";

                    db.SaveChanges();
                    
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"]     = "You don't have the rights to modify this project";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index");
                }
            }
            else // Invalid model state
            {
                ViewBag.Message = "Couldn't modify the project";
                ViewBag.Alert   = "alert-danger";

                return View(requestProject);
            }
        }

        public IActionResult Delete(int id)
        {
            Project project = db.Projects
                          .Where(p => p.Id == id)
                          .First();

            if (project.OrganizerId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                db.Projects.Remove(project);
                db.SaveChanges();

                TempData["message"]     = "The project was deleted";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"]     = "You don't have the rights to delete this project";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }

        private void SetAccessRights(Project project)
        {
            ViewBag.ShowButtons = false;

            // If the current user is the organizer, show the buttons
            if (project.OrganizerId == _userManager.GetUserId(User))
            {
                ViewBag.ShowButtons = true;
            }

            ViewBag.IsAdmin = User.IsInRole("Admin");
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllUsersFromProject(Project project)
        {
            // Select all user objects from all teams working on the project
            var users = from userTeam in db.UserTeams
                        join teamProject in db.TeamProjects on userTeam.TeamId equals teamProject.TeamId
                        where teamProject.ProjectId == project.Id
                        select userTeam.User;

            var selectList = new List<SelectListItem>();
            foreach (var user in users)
            {
                selectList.Add(new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = user.UserName.ToString()
                });
            }

            return selectList;
        }
    }
}
