using DragonsLegacy.Data;
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

        public ItemsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
                                  RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public IActionResult Index(string itemFilter = "All")
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            if (itemFilter == "NotOwned") // Select the items that the current user doesn't have
            {
                var items = from item in db.Items
                            where !(from userItem in db.UserItems
                                    where userItem.UserId == _userManager.GetUserId(User)
                                    select userItem.ItemId)
                                    .Contains(item.Id)
                            select item;

                ViewBag.Items = items;
                ViewBag.Count = items.Count();
            }
            else if (itemFilter == "Owned") // Select the items that the current user has
            {
                var items = from item in db.Items
                            join userItem in db.UserItems
                            on item.Id equals userItem.ItemId
                            where userItem.UserId == _userManager.GetUserId(User)
                            select item;

                ViewBag.Items = items;
                ViewBag.Count = items.Count();
            }
            else if (itemFilter == "All") // Select all items
            {
                var items = from item in db.Items
                            select item;

                ViewBag.Items = items;
                ViewBag.Count = items.Count();
            }
            else // Invalid item filter
            {
                TempData["message"] = "Invalid item filter";
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
        public IActionResult New(Item item)
        {
            var sanitizer = new HtmlSanitizer();
            if (ModelState.IsValid) // Add the item to the database
            {
                item.Description = sanitizer.Sanitize(item.Description);
                db.Items.Add(item);
                db.SaveChanges();

                TempData["message"] = "The item was successfully added";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "The item couldn't be added";
                ViewBag.Alert = "alert-danger";
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
            var sanitizer = new HtmlSanitizer();
            Item item = db.Items.Find(id);

            if (ModelState.IsValid) // Modify the item
            {
                requestItem.Description = sanitizer.Sanitize(requestItem.Description);
                item.Name = requestItem.Name;
                item.Description = requestItem.Description;
                item.Multimedia = requestItem.Multimedia;
                item.Price = requestItem.Price;
                item.NumberOfItems = requestItem.NumberOfItems;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
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
            return RedirectToAction("Index");
        }
    }
}
