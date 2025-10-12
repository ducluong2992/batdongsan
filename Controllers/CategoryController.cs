using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
