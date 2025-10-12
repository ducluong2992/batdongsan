using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class ProjectController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
