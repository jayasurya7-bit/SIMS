using Microsoft.AspNetCore.Mvc;

namespace SIMS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
