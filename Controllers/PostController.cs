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
            // 🌟 Lấy các bài đăng nổi bật
            var featuredPosts = await _context.Posts
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.ClickCount)
                .Take(5)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .Include(p => p.User) // ✅ Thêm để lấy thông tin người đăng
                .ToListAsync();

            ViewBag.FeaturedPosts = featuredPosts;

            // ❤️ Lấy danh sách bài đã yêu thích
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<int> favoriteIds = new();

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                favoriteIds = await _context.Prefereds
                    .Where(p => p.UserID == userId && p.PostID != null)
                    .Select(p => p.PostID!.Value)
                    .ToListAsync();
            }

            ViewBag.FavoritePostIds = favoriteIds;

            // 📋 Lấy tất cả bài đăng đã duyệt
            var allPosts = await _context.Posts
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreateAt)
                .Include(p => p.User) // ✅ Lấy thông tin người đăng
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .ToListAsync();

            return View(allPosts);
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

            // 🌟 Lấy danh sách bài đăng liên quan
            var relatedPosts = await _context.Posts
                .Where(p => p.Status == "Đã duyệt"
                         && p.PostID != id
                         && (p.CategoryID == post.CategoryID    // cùng loại nhà
                             || p.CommuneWard.DistrictID == post.CommuneWard.DistrictID)) // hoặc cùng khu vực
                .OrderByDescending(p => p.CreateAt)
                .Take(3)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .Include(p => p.User)
                .ToListAsync();

            ViewBag.RelatedPosts = relatedPosts;

            return View(post);
        }

        // --- 3. TRANG ĐĂNG BÀI ---
        [Authorize]
        public IActionResult Create()
        {
            ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
            ViewData["CategoryList"] = new SelectList(_context.Categories, "CategoryID", "CategoryName");

            // ✅ Tự động điền số điện thoại người đăng
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null)
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == int.Parse(currentUserId));
                ViewBag.UserPhone = user?.Phone ?? "";
            }

            return View();
        }

        // --- 4. POST: XỬ LÝ ĐĂNG BÀI ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Location,Area,Price,CommuneID,CategoryID,ContactPhone")] Post post,
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
                .Select(d => new { districtID = d.DistrictID, districtName = d.DistrictName })
                .ToListAsync();
            return Json(districts);
        }

        // --- 6. API lấy xã/phường ---
        [HttpGet]
        public async Task<JsonResult> GetCommunesByDistrictId(int districtId)
        {
            var communes = await _context.CommuneWards
                .Where(c => c.DistrictID == districtId)
                .Select(c => new { communeID = c.CommuneID, communeName = c.CommuneName })
                .ToListAsync();
            return Json(communes);
        }


        // --- 7. QUẢN LÝ TIN CỦA TÔI ---
        [Authorize]
        public async Task<IActionResult> MyPosts()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var myPosts = await _context.Posts
                .Where(p => p.UserID == currentUserId)
                .Include(p => p.Images.Take(1))
                .Include(p => p.Category)
                .Include(p => p.CommuneWard.District.Province)
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return View(myPosts);
        }

        [HttpGet("/api/Location/GetFullAddress")]
        public IActionResult GetFullAddress(int communeId)
        {
            var commune = _context.CommuneWards
                .Include(c => c.District)
                .ThenInclude(d => d.Province)
                .FirstOrDefault(c => c.CommuneID == communeId);

            if (commune == null)
                return NotFound();

            return Json(new
            {
                provinceID = commune.District?.Province?.ProvinceID,
                districtID = commune.District?.DistrictID,
                communeID = commune.CommuneID
            });
        }




        // --- 8. SỬA BÀI ĐĂNG ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.PostID == id && p.UserID == currentUserId);

            if (post == null)
                return NotFound();

            ViewData["CategoryList"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", post.CategoryID);
            ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
            ViewData["ProvinceList"] = _context.Provinces.ToList();


            return View(post);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostID,Title,Description,Location,Area,Price,CommuneID,CategoryID")] Post model, List<IFormFile>? newImages)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existingPost = await _context.Posts.Include(p => p.Images).FirstOrDefaultAsync(p => p.PostID == id && p.UserID == currentUserId);

            if (existingPost == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["CategoryList"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", model.CategoryID);
                ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
                return View(model);
            }

            // Cập nhật thông tin
            existingPost.Title = model.Title;
            existingPost.Description = model.Description;
            existingPost.Location = model.Location;
            existingPost.Area = model.Area;
            existingPost.Price = model.Price;
            existingPost.CategoryID = model.CategoryID;
            existingPost.CommuneID = model.CommuneID;
            existingPost.Status = "Chờ duyệt"; // sau khi sửa thì quay lại chờ duyệt
            existingPost.CreateAt = DateTime.Now;

            // Thêm ảnh mới nếu có
            if (newImages != null && newImages.Count > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                foreach (var imageFile in newImages)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    var img = new Image
                    {
                        ImageUrl = "/images/posts/" + uniqueFileName,
                        PostID = existingPost.PostID
                    };
                    _context.Images.Add(img);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bài đăng đã được cập nhật và gửi lại để duyệt.";

            return RedirectToAction(nameof(MyPosts));
        }

        // --- 9. XÓA BÀI ĐĂNG ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.PostID == id && p.UserID == currentUserId);

            if (post == null)
                return NotFound();

            // Xóa file ảnh vật lý
            if (post.Images != null)
            {
                foreach (var img in post.Images)
                {
                    var path = Path.Combine(_webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bài đăng đã được xóa thành công!";
            return RedirectToAction(nameof(MyPosts));
        }

    }
}
