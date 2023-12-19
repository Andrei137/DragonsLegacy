using DragonsLegacy.Data;
using DragonsLegacy.Models;
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
            Comment comment = db.Comments.Find(id);

            if (ModelState.IsValid) // Modify the comment
            {
                comment.Content = requestComment.Content;
                db.SaveChanges();

                return Redirect("/Tasks/Show/" + comment.TaskId);
            }
            else // Invalid model state
            {
                return View(comment);
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            Comment comment = db.Comments.Find(id);

            db.Comments.Remove(comment);
            db.SaveChanges();

            return Redirect("/Tasks/Show/" + comment.TaskId);
        }
    }
}
