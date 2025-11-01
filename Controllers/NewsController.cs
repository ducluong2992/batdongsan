using bds.Data;
using bds.Models;
using bds.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bds.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly LogService _logService;

        public NewsController(ApplicationDbContext context, IWebHostEnvironment env, LogService logService)
        {
            _context = context;
            _env = env;
            _logService = logService;
        }

        // GET: News
        public IActionResult Index()
        {
            var newsList = _context.News
                .Include(n => n.User)
                .Include(n => n.Images)
                .OrderByDescending(n => n.CreateAt)
                .ToList();

            ViewData["Layout"] = User.IsInRole("Admin")
                ? "~/Views/Shared/_LayoutAdmin.cshtml"
                : "~/Views/Shared/_Layout.cshtml";

            return View(newsList);
        }

        // GET: News/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            ViewData["Layout"] = "~/Views/Shared/_LayoutAdmin.cshtml";
            return View();
        }

        // POST: News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News model, List<IFormFile>? imageFiles)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logService.AddLog(null, "CreateNews", "Lỗi: User chưa đăng nhập", "News", null, false);
                return Content("❌ Lỗi: User chưa đăng nhập");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                _logService.AddLog(null, "CreateNews", "Lỗi: Không tìm thấy user", "News", null, false);
                return Content("❌ Lỗi: Không tìm thấy user");
            }

            model.UserID = user.UserID;
            model.CreateAt = DateTime.Now;
            model.ViewCount = 0;

            try
            {
                _context.News.Add(model);
                await _context.SaveChangesAsync();

                // Upload ảnh nếu có
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    string uploadDir = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "images", "news");
                    Directory.CreateDirectory(uploadDir);

                    foreach (var file in imageFiles)
                    {
                        if (file == null || file.Length == 0) continue;

                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string ext = Path.GetExtension(file.FileName);
                        string safeFileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
                        string uniqueFileName = Guid.NewGuid() + "_" + safeFileName + ext;
                        string filePath = Path.Combine(uploadDir, uniqueFileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.Images.Add(new Image
                        {
                            NewsID = model.NewsID,
                            ImageUrl = "/images/news/" + uniqueFileName
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                _logService.AddLog(user.UserID, "CreateNews", $" {user.Username} tạo tin tức: {model.Title}", "News", model.NewsID, true);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logService.AddLog(user.UserID, "CreateNews", $"Lỗi khi tạo tin tức: {ex.Message}", "News", null, false);
                return Content("❌ Lỗi khi tạo tin tức: " + ex.Message);
            }
        }

        // GET: News/Details/5
        public IActionResult Details(int id)
        {
            var news = _context.News
                .Include(n => n.User)
                .Include(n => n.Images)
                .FirstOrDefault(n => n.NewsID == id);

            if (news == null)
                return NotFound();

            try
            {
                news.ViewCount += 1;
                _context.SaveChanges();

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? userId = userIdStr != null ? int.Parse(userIdStr) : null;

                _logService.AddLog(userId, "ViewNews", $"Xem chi tiết tin tức: {news.Title}", "News", news.NewsID, true);
            }
            catch { }

            ViewData["Layout"] = User.IsInRole("Admin")
                ? "~/Views/Shared/_LayoutAdmin.cshtml"
                : "~/Views/Shared/_Layout.cshtml";

            return View(news);
        }

        // GET: News/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            ViewData["Layout"] = "~/Views/Shared/_LayoutAdmin.cshtml";
            return View(news);
        }

        // POST: News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NewsID,Title,Content")] News model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            if (id != model.NewsID) return NotFound();

            if (!ModelState.IsValid) return View(model);

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            try
            {
                var existingNews = await _context.News.FindAsync(id);
                if (existingNews == null) return NotFound();

                existingNews.Title = model.Title;
                existingNews.Content = model.Content;
                existingNews.CreateAt = DateTime.Now;
                if (user != null) existingNews.UserID = user.UserID;

                _context.Update(existingNews);
                await _context.SaveChangesAsync();

                _logService.AddLog(user?.UserID, "EditNews", $"{user.Username}  xóa tin tức: : {existingNews.Title}", "News", existingNews.NewsID, true);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logService.AddLog(user?.UserID, "EditNews", $"Lỗi khi chỉnh sửa tin tức: {ex.Message}", "News", model.NewsID, false);
                return Content("❌ Lỗi khi cập nhật: " + ex.Message);
            }
        }

        // POST: News/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var news = await _context.News.Include(n => n.Images).FirstOrDefaultAsync(n => n.NewsID == id);
            if (news == null) return NotFound();

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            try
            {
                if (news.Images != null)
                {
                    foreach (var img in news.Images)
                    {
                        var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }

                    _context.Images.RemoveRange(news.Images);
                }

                _context.News.Remove(news);
                await _context.SaveChangesAsync();

                _logService.AddLog(user?.UserID, "DeleteNews", $"{user.Username} xóa tin tức: {news.Title}", "News", news.NewsID, true);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logService.AddLog(user?.UserID, "DeleteNews", $"Lỗi khi xóa tin tức: {ex.Message}", "News", news.NewsID, false);
                return Content("❌ Lỗi khi xóa: " + ex.Message);
            }
        }
    }
}
