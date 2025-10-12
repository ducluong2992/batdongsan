using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class NewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
