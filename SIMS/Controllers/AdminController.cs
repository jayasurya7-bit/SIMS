using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Attributes;
using System.Linq;

namespace SIMS.Controllers
{
    [RoleAuthorize(1)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db;
        public AdminController(ApplicationDbContext _db)
        {
            db = _db;
        }

        public IActionResult Dashboard(string statusFilter, string searchTerm, string sortOrder)
        {
            var incidents = db.GetAllIncidents();

            // Calculate stats for cards
            ViewBag.PendingCount = incidents.Count(i => i.Status == "Pending");
            ViewBag.InProgressCount = incidents.Count(i =>
                i.Status == "Assigned" || i.Status == "Investigating" || i.Status == "On Hold");
            ViewBag.ResolvedCount = incidents.Count(i =>
                i.Status == "Resolved" || i.Status == "Archived-Resolved");

            // Filter out archived items
            var displayList = incidents.Where(i => !i.Status.StartsWith("Archived")).ToList();

            // Apply status filter (including Unassigned)
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Unassigned")
                    displayList = displayList.Where(i => i.Status == "Pending" &&
                        (string.IsNullOrEmpty(i.InvestigatorName) || i.InvestigatorName == "Not Assigned")).ToList();
                else
                    displayList = displayList.Where(i => i.Status == statusFilter).ToList();
            }

            // Apply search filter (added by Archives Module)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                displayList = displayList.Where(i =>
                    i.Title.ToLower().Contains(searchTerm) ||
                    i.Description?.ToLower().Contains(searchTerm) == true ||
                    i.Category.ToLower().Contains(searchTerm) ||
                    i.ReporterName?.ToLower().Contains(searchTerm) == true ||
                    i.InvestigatorName?.ToLower().Contains(searchTerm) == true ||
                    i.IncidentID.ToString().Contains(searchTerm)
                ).ToList();
            }

            // Apply sorting (added by Archives Module)
            ViewBag.CurrentSort = sortOrder;
            ViewBag.IdSortParam = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.TitleSortParam = sortOrder == "title" ? "title_desc" : "title";
            ViewBag.DateSortParam = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.StatusSortParam = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.PrioritySortParam = sortOrder == "priority" ? "priority_desc" : "priority";

            displayList = sortOrder switch
            {
                "id_desc" => displayList.OrderByDescending(i => i.IncidentID).ToList(),
                "title" => displayList.OrderBy(i => i.Title).ToList(),
                "title_desc" => displayList.OrderByDescending(i => i.Title).ToList(),
                "date" => displayList.OrderBy(i => i.CreatedAt).ToList(),
                "date_desc" => displayList.OrderByDescending(i => i.CreatedAt).ToList(),
                "status" => displayList.OrderBy(i => i.Status).ToList(),
                "status_desc" => displayList.OrderByDescending(i => i.Status).ToList(),
                "priority" => displayList.OrderBy(i => i.Priority).ToList(),
                "priority_desc" => displayList.OrderByDescending(i => i.Priority).ToList(),
                _ => displayList.OrderBy(i => i.Status == "Resolved" || i.Status == "Denied")
                               .ThenByDescending(i => i.CreatedAt).ToList()
            };

            ViewBag.SelectedFilter = statusFilter;
            ViewBag.CurrentSearchTerm = searchTerm; // Added for search persistence
            ViewBag.Investigators = db.GetAllInvestigators();

            return View(displayList);
        }

        [HttpGet]
        public JsonResult GetChartData()
        {
            var all = db.GetAllIncidents();
            var data = new[]
            {
                new { label = "Pending", count = all.Count(i => i.Status == "Pending") },
                new { label = "In Progress", count = all.Count(i =>
                    i.Status == "Assigned" || i.Status == "Investigating" || i.Status == "On Hold") },
                new { label = "Resolved", count = all.Count(i =>
                    i.Status == "Resolved" || i.Status == "Archived-Resolved") }
            };
            return Json(data);
        }

        public IActionResult Deny(int id)
        {
            db.UpdateIncidentStatus(id, "Denied");
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            bool success = db.UpdateIncidentStatus(id, "Archived");
            return Json(new { success = success });
        }

        [HttpPost]
        public IActionResult Cancel(int id)
        {
            bool success = db.CancelAssignment(id);
            return Json(new { success = success });
        }

        public IActionResult Archives()
        {
            var allData = db.GetAllIncidents();
            var archivedReports = allData.Where(i => i.Status.StartsWith("Archived")).ToList();
            return View(archivedReports);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            bool success = db.UpdateIncidentStatus(id, "Restore");
            return Json(new { success = success });
        }

        [HttpPost]
        public IActionResult Assign(int incidentId, int investigatorId)
        {
            db.AssignIncident(incidentId, investigatorId);
            return RedirectToAction("Dashboard");
        }
    }
}