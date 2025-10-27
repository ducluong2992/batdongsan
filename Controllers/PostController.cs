using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bds.Controllers
{
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 1. DANH SÁCH BÀI ĐĂNG ---
        public async Task<IActionResult> Index()
        {
            var post = await _context.Posts
                .Where(p => p.Status == "Đã duyệt")
                .Include(p => p.Images.Take(1))
                .Include(p => p.Category)
                .Include(p => p.CommuneWard.District.Province)
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return View(post);
        }

        // --- 2. TRANG CHI TIẾT BÀI ĐĂNG ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.CommuneWard.District.Province)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.PostID == id);

            if (post == null || post.Status != "Đã duyệt")
                return NotFound();

            // Cộng lượt xem
            post.ClickCount = (post.ClickCount ?? 0) + 1;
            _context.Update(post);
            await _context.SaveChangesAsync();

            return View(post);
        }

        // --- 3. TRANG ĐĂNG BÀI ---
        [Authorize]
        public IActionResult Create()
        {
            ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
            ViewData["CategoryList"] = new SelectList(_context.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // --- 4. POST: XỬ LÝ ĐĂNG BÀI ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Location,Area,Price,CommuneID,CategoryID")] Post post,
            List<IFormFile>? images)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
                return RedirectToAction("Login", "Account");

            post.UserID = int.Parse(currentUserId);
            post.Status = "Chờ duyệt";
            post.CreateAt = DateTime.Now;
            post.ClickCount = 0;

            if (!ModelState.IsValid)
            {
                ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
                ViewData["CategoryList"] = new SelectList(_context.Categories, "CategoryID", "CategoryName");
                return View(post);
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // --- Upload ảnh ---
            if (images != null && images.Count > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                foreach (var imageFile in images)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    var image = new Image
                    {
                        ImageUrl = "/images/posts/" + uniqueFileName,
                        PostID = post.PostID
                    };
                    _context.Images.Add(image);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Bài đăng của bạn đã được gửi thành công! Hãy chờ quản trị viên duyệt nhé.";
            return RedirectToAction("Create");
        }

        // --- 5. API lấy quận/huyện ---
        [HttpGet]
        public async Task<JsonResult> GetDistrictsByProvinceId(int provinceId)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceID == provinceId)
                .Select(d => new { id = d.DistrictID, name = d.DistrictName })
                .ToListAsync();
            return Json(districts);
        }

        // --- 6. API lấy xã/phường ---
        [HttpGet]
        public async Task<JsonResult> GetCommunesByDistrictId(int districtId)
        {
            var communes = await _context.CommuneWards
                .Where(c => c.DistrictID == districtId)
                .Select(c => new { id = c.CommuneID, name = c.CommuneName })
                .ToListAsync();
            return Json(communes);
        }
    }
}
