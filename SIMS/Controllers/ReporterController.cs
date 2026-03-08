using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Models;
using SIMS.Attributes;

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
                string[] allowedMimeTypes = { "image/jpeg", "image/png" };
                string extension = Path.GetExtension(EvidenceFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension) || !allowedMimeTypes.Contains(EvidenceFile.ContentType))
                {
                    ModelState.AddModelError("EvidenceFile", "Only JPG, JPEG, and PNG image files are allowed.");
                    return View(inc);
                }

                if (EvidenceFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("EvidenceFile", "File size must be less than 5 MB.");
                    return View(inc);
                }

                string uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

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
            {
                TempData["SuccessMessage"] = "Incident reported successfully with evidence!";
                return RedirectToAction("MyReports");
            }

            ModelState.AddModelError("", "An error occurred while saving the report.");
            return View(inc);
        }
    }
}