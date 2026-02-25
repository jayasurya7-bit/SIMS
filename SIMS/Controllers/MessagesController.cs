using Microsoft.AspNetCore.Mvc;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;
        public MessagesController(ApplicationDbContext _db)
        {
            db = _db;
        }

        public IActionResult Chat(int incidentId)
        {
            string uid = HttpContext.Session.GetString("UID");
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Index", "Home");

            ViewBag.IncidentID = incidentId;
            ViewBag.CurrentUserID = int.Parse(uid);

            var messages = db.GetMessages(incidentId);
            return View(messages);
        }
        [HttpPost]
        public IActionResult Send(int incidentId, string messageBody)
        {
            string uidStr = HttpContext.Session.GetString("UID");

            if (string.IsNullOrEmpty(uidStr))
            {
                return RedirectToAction("Login", "Account");
            }

            int senderId = int.Parse(uidStr);

            var msgObj = new InternalMessage
            {
                IncidentID = incidentId,
                SenderID = senderId,
                MessageBody = messageBody
            };

            db.SendMessage(msgObj);

            return RedirectToAction("Chat", new { incidentId = incidentId });
        }
    }
}