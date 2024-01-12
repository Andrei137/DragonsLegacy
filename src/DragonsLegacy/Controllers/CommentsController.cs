using DragonsLegacy.Data;
using DragonsLegacy.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;

namespace DragonsLegacy.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;

        public CommentsController(ApplicationDbContext context)
        {
            db = context;
        }

        // GET
        public IActionResult Edit(int id)
        {
            Comment comment = db.Comments.Find(id);
            return View(comment);
        }

        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            Comment comment = db.Comments.Find(id);

            if (ModelState.IsValid) // Modify the comment
            {
                var sanitizer = new HtmlSanitizer();
                requestComment.Content = sanitizer.Sanitize(requestComment.Content);
                comment.Content = requestComment.Content;
                TempData["message"] = "The comment was successfully modified";
                TempData["messageType"] = "alert-success";
                db.SaveChanges();

                return Redirect("/Tasks/Show/" + comment.TaskId);
            }
            else // Invalid model state
            {
                TempData["message"] = "The comment couldn't be modified";
                TempData["messageType"] = "alert-danger";
                return View(comment);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            Comment comment = db.Comments.Find(id);

            TempData["message"] = "The comment was successfully deleted";
            TempData["messageType"] = "alert-success";

            db.Comments.Remove(comment);
            db.SaveChanges();

            return Redirect("/Tasks/Show/" + comment.TaskId);
        }
    }
}
