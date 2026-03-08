using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
// REMOVED: using SIMS.Helpers;  // PasswordHelper is gone

namespace SIMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext db;
        public AccountController(ApplicationDbContext _db)
        {
            db = _db;
        }
        public IActionResult AdminLogin() => View("../Admin/AdminLogin");
        public IActionResult InvestigatorLogin() => View("../Investigator/InvestigatorLogin");
        public IActionResult ReporterLogin() => View("../Reporter/ReporterLogin");

        [HttpPost]
        public IActionResult ProcessLogin(string u, string p, int expectedRole)
        {
            // REMOVED: string hashedPassword = PasswordHelper.HashPassword(p);
            // Now using plain password directly
            var user = db.ValidateUser(u, p);  // Passing plain password, not hash

            if (user != null && user.RoleID == expectedRole)
            {
                HttpContext.Session.SetString("UID", user.UserID.ToString());
                HttpContext.Session.SetString("RID", user.RoleID.ToString());
                HttpContext.Session.SetString("UName", user.Username);

                if (user.RoleID == 1) return RedirectToAction("Dashboard", "Admin");
                if (user.RoleID == 2) return RedirectToAction("MyReports", "Reporter");
                if (user.RoleID == 3) return RedirectToAction("MyTasks", "Investigator");
            }

            ViewBag.Error = "Invalid Login credentials for this portal!";

            if (expectedRole == 1) return View("../Admin/AdminLogin");
            if (expectedRole == 3) return View("../Investigator/InvestigatorLogin");
            return View("../Reporter/ReporterLogin");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}