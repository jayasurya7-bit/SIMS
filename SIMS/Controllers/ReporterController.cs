using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    public class ReporterController : Controller
    {
        private readonly ApplicationDbContext db;

        public ReporterController(ApplicationDbContext _db)
        {
            db = _db;
        }
        public IActionResult MyReports()
        {
            string uid = HttpContext.Session.GetString("UID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("ReporterLogin", "Account");

            var reports = db.GetIncidentsByReporter(int.Parse(uid));
            return View(reports);
        }

        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UID")))
                return RedirectToAction("ReporterLogin", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Incident inc, string Location, IFormFile EvidenceFile)
        {
            var userIdStr = HttpContext.Session.GetString("UID");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("ReporterLogin", "Account");

            if (EvidenceFile != null && EvidenceFile.Length > 0)
            {
                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid().ToString() + "_" + EvidenceFile.FileName;
                string filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    EvidenceFile.CopyTo(stream);
                }
                inc.FilePath = "/uploads/" + fileName; 
            }

            inc.ReportedBy = int.Parse(userIdStr);
            inc.Location = Location;

            if (db.CreateIncident(inc)) return RedirectToAction("MyReports");
            return View(inc);
        }
    }
}