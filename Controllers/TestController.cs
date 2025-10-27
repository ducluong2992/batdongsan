using bds.Data;
using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var count = _context.Posts.Count();
            return Content($"Kết nối thành công! Có {count} bài đăng trong database.");
        }
    }
}
