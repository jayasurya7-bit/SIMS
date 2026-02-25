using Microsoft.AspNetCore.Mvc;
using SIMS.Data;

namespace SIMS.Controllers
{
    public class InvestigatorController : Controller
    {
        private readonly ApplicationDbContext db;

        public InvestigatorController(ApplicationDbContext _db)
        {
            db = _db;
        }
        public IActionResult MyTasks()
        {
            string uid = HttpContext.Session.GetString("UID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("InvestigatorLogin", "Account");

            var tasks = db.GetAssignedIncidents(int.Parse(uid));
            return View(tasks);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int incId, string status)
        {
            bool success = db.UpdateIncidentStatus(incId, status);
            if (success)
            {
                TempData["Message"] = "Status successfully changed!";
            }
            return RedirectToAction("MyTasks");
        }
    }
}