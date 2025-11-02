using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bds.Controllers
{
    [Authorize]
    public class PreferedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PreferedController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(int projectId)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existing = await _context.Prefereds
                .FirstOrDefaultAsync(p => p.UserID == userId && p.ProjectID == projectId);

            bool isFavorited;
            if (existing != null)
            {
                _context.Prefereds.Remove(existing);
                isFavorited = false;
            }
            else
            {
                _context.Prefereds.Add(new Prefered { UserID = userId, ProjectID = projectId });
                isFavorited = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isFavorited = isFavorited });
        }

        // Trang hiển thị danh sách yêu thích
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // ✅ Lấy danh sách tin đăng yêu thích
            var preferedPosts = await _context.Prefereds
                .Where(p => p.UserID == userId && p.PostID != null)
                .Include(p => p.Post).ThenInclude(x => x.Images)
                .Select(p => p.Post!)
                .ToListAsync();

            // ✅ Lấy danh sách dự án yêu thích
            var preferedProjects = await _context.Prefereds
                .Where(p => p.UserID == userId && p.ProjectID != null)
                .Include(p => p.Project).ThenInclude(x => x.Images)
                .Include(p => p.Project).ThenInclude(x => x.CommuneWard)
                    .ThenInclude(c => c.District).ThenInclude(d => d.Province)
                .Select(p => p.Project!)
                .ToListAsync();

            // ✅ Truyền dữ liệu sang ViewBag
            ViewBag.PreferedPosts = preferedPosts;
            ViewBag.PreferedProjects = preferedProjects;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavoritePost(int postId)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existing = await _context.Prefereds
                .FirstOrDefaultAsync(p => p.UserID == userId && p.PostID == postId);

            bool isFavorited;
            if (existing != null)
            {
                _context.Prefereds.Remove(existing);
                isFavorited = false;
            }
            else
            {
                _context.Prefereds.Add(new Prefered { UserID = userId, PostID = postId });
                isFavorited = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isFavorited });
        }


    }
}
