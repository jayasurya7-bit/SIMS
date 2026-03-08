using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Attributes;

namespace SIMS.Controllers
{
    [RoleAuthorize(3)]
    public class InvestigatorController : Controller
    {
        private readonly ApplicationDbContext db;

        public InvestigatorController(ApplicationDbContext _db)
        {
            db = _db;
        }

        public IActionResult MyTasks()
        {
            string uidStr = HttpContext.Session.GetString("UID");

            if (!int.TryParse(uidStr, out int uid))
            {
                return RedirectToAction("Index", "Home");
            }

            var allTasks = db.GetAssignedIncidents(uid);

            var activeTasks = allTasks.Where(t => t.Status == "Assigned" ||
                                                  t.Status == "Investigating" ||
                                                  t.Status == "On Hold").ToList();

            var resolvedTasks = allTasks.Where(t => t.Status == "Resolved" ||
                                                    t.Status == "Archived-Resolved").ToList();

            ViewBag.ActiveCount = activeTasks.Count;
            ViewBag.ResolvedCount = resolvedTasks.Count;
            ViewBag.TotalCount = activeTasks.Count + resolvedTasks.Count;
            ViewBag.ResolvedTasks = resolvedTasks;

            return View(activeTasks);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int incId, string status)
        {
            string[] allowedStatuses = { "Investigating", "On Hold", "Resolved" };
            if (!allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid status update.");
            }

            bool success = db.UpdateIncidentStatus(incId, status);
            if (success)
            {
                TempData["Message"] = "Status successfully changed!";
            }
            return RedirectToAction("MyTasks");
        }
    }
}