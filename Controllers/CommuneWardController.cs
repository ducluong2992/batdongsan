using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class CommuneWardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
