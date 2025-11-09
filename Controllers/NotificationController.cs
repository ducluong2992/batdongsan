using bds.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bds.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.Post)
                .ToListAsync();

            return View(notifications);
        }

        // 🔹 Lấy 5 thông báo mới nhất
        [HttpGet]
        public async Task<JsonResult> GetLatest()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

            int userId = int.Parse(userIdStr);

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.NotificationID,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt,
                    n.PostID,
                    n.ProjectID
                })
                .ToListAsync();

            return Json(new { success = true, data = notifications });
        }

        // ✅ Đánh dấu 1 thông báo là đã đọc
        [HttpPost]
        public async Task<JsonResult> MarkAsRead(int id)
        {
            var noti = await _context.Notifications.FindAsync(id);
            if (noti == null) return Json(new { success = false });

            noti.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ✅ Đánh dấu tất cả là đã đọc
        [HttpPost]
        public async Task<JsonResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                unread.ForEach(n => n.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, count = unread.Count });
        }
    }
}
