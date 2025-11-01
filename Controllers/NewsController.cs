using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bds.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

            // Lấy User hiện tại
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Content("❌ Lỗi: User chưa đăng nhập");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Content("❌ Lỗi: Không tìm thấy user");

            model.UserID = user.UserID;
            model.CreateAt = DateTime.Now;
            model.ViewCount = 0;

            // Lưu News trước để có NewsID
            _context.News.Add(model);
            await _context.SaveChangesAsync();

            // Upload ảnh nếu có
            if (imageFiles != null && imageFiles.Count > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "images", "news");
                Directory.CreateDirectory(uploadDir);

                foreach (var file in imageFiles)
                {
                    if (file == null || file.Length == 0)
                        continue;

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
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            var news = _context.News
                .Include(n => n.User)
                .Include(n => n.Images)
                .FirstOrDefault(n => n.NewsID == id);

            if (news == null)
                return NotFound();
            news.ViewCount += 1;
            _context.SaveChanges();
            ViewData["Layout"] = User.IsInRole("Admin")
                ? "~/Views/Shared/_LayoutAdmin.cshtml"
                : "~/Views/Shared/_Layout.cshtml";

            return View(news);
        }

        // GET: News/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var news = await _context.News.FindAsync(id);
            if (news == null)
                return NotFound();

            ViewData["Layout"] = "~/Views/Shared/_LayoutAdmin.cshtml";
            return View(news);
        }

        // POST: News/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NewsID,Title,Content")] News model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            if (id != model.NewsID)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingNews = await _context.News.FindAsync(id);
                if (existingNews == null)
                    return NotFound();

                // Cập nhật các trường được phép sửa
                existingNews.Title = model.Title;
                existingNews.Content = model.Content;

                // Cập nhật thời gian chỉnh sửa
                existingNews.CreateAt = DateTime.Now;

                // Cập nhật UserID người sửa (nếu có đăng nhập)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                    existingNews.UserID = user.UserID;

                _context.Update(existingNews);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content("❌ Lỗi khi cập nhật: " + ex.Message);
            }
        }


        // POST: News/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var news = await _context.News
                .Include(n => n.Images)
                .FirstOrDefaultAsync(n => n.NewsID == id);

            if (news == null)
                return NotFound();

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

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content("❌ Lỗi khi xóa: " + ex.Message);
            }
        }

    }
}

