using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class SearchHistoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
