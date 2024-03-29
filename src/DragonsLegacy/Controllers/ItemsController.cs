﻿using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DragonsLegacy.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IWebHostEnvironment _env;

        public ItemsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
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
                ViewBag.Alert   = TempData["messageType"];
            }

            var items = from item in db.Items
                        select item;

            // Filter
            var itemFilter = "AllItems";

            if (Convert.ToString(HttpContext.Request.Query["itemFilter"]) != null)
            {
                itemFilter = Convert.ToString(HttpContext.Request.Query["itemFilter"]).Trim();
            }

            if (itemFilter == "NotOwnedItems") // Select the items that the current user doesn't have
            {
                items = from item in db.Items
                        where !(
                                    from userItem in db.UserItems
                                    where userItem.UserId == _userManager.GetUserId(User)
                                    select userItem.ItemId
                               ).Contains(item.Id)
                        select item;
            }
            else if (itemFilter == "OwnedItems") // Select the items that the current user has
            {
                items = from item in db.Items
                        join userItem in db.UserItems on item.Id equals userItem.ItemId
                        where userItem.UserId == _userManager.GetUserId(User)
                        select item;
            }
            else if (itemFilter != "AllItems") // Invalid item filter
            {
                TempData["message"]     = "Invalid item filter";
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
                items = items
                        .Where(i => i.Name.Contains(search) || i.Description.Contains(search));
            }

            int perPage     = 9;
            int totalItems  = items.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset      = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * perPage;
            }
             
            ViewBag.IsAdmin      = User.IsInRole("Admin");
            ViewBag.Items        = items.Skip(offset).Take(perPage);
            ViewBag.Count        = totalItems;
            ViewBag.ItemFilter   = itemFilter;
            ViewBag.SearchString = search;
            ViewBag.LastPage     = Math.Ceiling((float)totalItems / (float)perPage);

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Items/Index/?itemFilter=" + itemFilter + 
                                            "&search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Items/Index/?itemFilter=" + itemFilter + "&page";
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

            Item item = db.Items
                          .Where(i => i.Id == id)
                          .First();

            ViewBag.IsAdmin = User.IsInRole("Admin");

            return View(item);
        }

        // GET 
        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            Item item = new Item();

            return View(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> New(Item item, IFormFile? Media)
        {
            if (ModelState.IsValid) // Add the item to the database
            {
                if (Media != null && Media.Length > 0)
                {
                    var storagePath      = Path.Combine(_env.WebRootPath, "items", Media.FileName);
                    var databaseFileName = "/items/" + Media.FileName;

                    using (var fileStream = new FileStream(storagePath, FileMode.Create))
                    {
                        await Media.CopyToAsync(fileStream);
                    }

                    item.Multimedia = databaseFileName;
                }

                var sanitizer           = new HtmlSanitizer();
                item.Description        = sanitizer.Sanitize(item.Description);
                TempData["message"]     = "The item was successfully added";
                TempData["messageType"] = "alert-success";

                db.Items.Add(item);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The item couldn't be added";
                ViewBag.Alert   = "alert-danger";

                return View(item);
            }
        }

        // GET
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            Item item = db.Items.Find(id);

            return View(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Item requestItem)
        {
            Item item = db.Items.Find(id);

            if (ModelState.IsValid) // Modify the item
            {
                var sanitizer           = new HtmlSanitizer();
                requestItem.Description = sanitizer.Sanitize(requestItem.Description);
                item.Name               = requestItem.Name;
                item.Description        = requestItem.Description;
                item.Multimedia         = requestItem.Multimedia;
                item.Price              = requestItem.Price;
                item.NumberOfItems      = requestItem.NumberOfItems;
                TempData["message"]     = "The item was successfully modified";
                TempData["messageType"] = "alert-success";

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The item couldn't be modified";
                ViewBag.Alert   = "alert-danger";

                return View(requestItem);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Item item = db.Items.Find(id);
            db.Items.Remove(item);
            db.SaveChanges();

            TempData["message"]     = "The item was successfully deleted";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }
    }
}
