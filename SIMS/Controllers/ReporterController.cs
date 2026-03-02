using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Models;
using Microsoft.AspNetCore.Hosting;

namespace SIMS.Controllers
{
    [RoleAuthorize(2)]
    public class ReporterController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;

        public ReporterController(ApplicationDbContext _db, IWebHostEnvironment env)
        {
            db = _db;
            _env = env;
        }

        public IActionResult MyReports()
        {
            string uid = HttpContext.Session.GetString("UID");
            var reports = db.GetIncidentsByReporter(int.Parse(uid));
            return View(reports);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Incident inc, string Location, IFormFile EvidenceFile)
        {
            string userIdStr = HttpContext.Session.GetString("UID");

            if (EvidenceFile != null && EvidenceFile.Length > 0)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(EvidenceFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Only JPG, JPEG, and PNG image files are allowed.");
                    return View(inc);
                }

                if (EvidenceFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File size must be less than 5 MB.");
                    return View(inc);
                }

                string uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    EvidenceFile.CopyTo(stream);
                }

                inc.FilePath = "/uploads/" + fileName;
            }

            inc.ReportedBy = int.Parse(userIdStr);
            inc.Location = Location;

            if (db.CreateIncident(inc))
                return RedirectToAction("MyReports");

            return View(inc);
        }
    }
}