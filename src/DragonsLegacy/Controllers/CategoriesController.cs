using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArticlesApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        public CategoriesController(ApplicationDbContext context)
        {
            db = context;
        }

        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // Select all categories in alphabetical order
            var categories = from category in db.Categories
                             orderby category.Name
                             select category;
            ViewBag.Categories = categories;
            return View();
        }

        public ActionResult Show(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        // GET
        public ActionResult New()
        {
            Category category = new Category();
            return View(category);
        }

        [HttpPost]
        public ActionResult New(Category category)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            if (ModelState.IsValid) // Add the category to the database
            {
                db.Categories.Add(category);
                db.SaveChanges();
                TempData["message"] = "The category was successfully added";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                TempData["message"] = "The category couldn't be added";
                TempData["messageType"] = "alert-danger";
                return View(category);
            }
        }

        public ActionResult Edit(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        [HttpPost]
        public ActionResult Edit(int id, Category requestCategory)
        {
            Category category = db.Categories.Find(id);
            if (ModelState.IsValid) // Modify the category
            {
                category.Name = requestCategory.Name;
                db.SaveChanges();
                TempData["message"] = "The category was successfully modified";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else // Invalid model state
            {
                ViewBag.Message = "Couldn't modify the category";
                return View(requestCategory);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            Category category = db.Categories.Find(id);
            db.Categories.Remove(category);
            db.SaveChanges();
            TempData["message"] = "The category was deleted";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }
    }
}
