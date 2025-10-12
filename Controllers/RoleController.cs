using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class RoleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
