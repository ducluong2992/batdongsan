using bds.Data;
using bds.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bds.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- TRANG HỒ SƠ NGƯỜI DÙNG ---
        public async Task<IActionResult> Profile(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound();

            // 🔹 Lấy các bài đăng đã duyệt
            var posts = await _context.Posts
                .Where(p => p.UserID == userId && p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreateAt)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .ToListAsync();

            // 🔹 Lấy các dự án đã duyệt
            var projects = await _context.Projects
                .Where(p => p.UserID == userId && p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreateAt)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .ToListAsync();

            var viewModel = new UserProfileViewModel
            {
                User = user,
                Posts = posts,
                Projects = projects
            };

            return View(viewModel);
        }
    }
}
