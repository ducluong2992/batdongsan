using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class StatisticController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
