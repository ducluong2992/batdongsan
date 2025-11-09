using System.Diagnostics;
using System.Security.Claims;
using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace bds.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.Provinces = _context.Provinces
                .OrderBy(p => p.ProvinceName)
                .ToList();

            ViewBag.Categories = _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            // 🔹 Top bài viết
            var topPosts = _context.Posts
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.ClickCount)
                .Take(6)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province)
                .Include(p => p.User)
                .ToList();

            ViewBag.TopPosts = topPosts;

            // 🔹 Top dự án
            var topProjects = _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.ClickCount)
                .Take(6)
                .Include(p => p.Images.Take(1))
                .Include(p => p.User)
                .ToList();

            ViewBag.TopProjects = topProjects;

            // --- Lấy UserID hiện tại ---
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<int> favoritePostIds = new();
            List<int> favoriteProjectIds = new();

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                // ❤️ Lấy danh sách bài post đã tym
                favoritePostIds = _context.Prefereds
                    .Where(p => p.UserID == userId && p.PostID != null)
                    .Select(p => p.PostID.Value)
                    .ToList();

                // ❤️ Lấy danh sách project đã tym
                favoriteProjectIds = _context.Prefereds
                    .Where(p => p.UserID == userId && p.ProjectID != null)
                    .Select(p => p.ProjectID.Value)
                    .ToList();
            }

            ViewBag.FavoritePostIds = favoritePostIds;
            ViewBag.FavoriteProjectIds = favoriteProjectIds;


            // 🔹 Top tin tức
            var topNews = _context.News
                .OrderByDescending(n => n.ViewCount)
                .Take(6)
                .Include(n => n.Images.Take(1))
                .Include(n => n.User)
                .ToList();

            ViewBag.TopNews = topNews;

            return View();
        }


        // --- API load Quận/Huyện theo Tỉnh ---
        [HttpGet]
        public async Task<JsonResult> GetDistrictsByProvinceId(int provinceId)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceID == provinceId)
                .Select(d => new { id = d.DistrictID, name = d.DistrictName })
                .ToListAsync();

            return Json(districts);
        }

        // --- API load Phường/Xã theo Quận ---
        [HttpGet]
        public async Task<JsonResult> GetCommunesByDistrictId(int districtId)
        {
            var communes = await _context.CommuneWards
                .Where(c => c.DistrictID == districtId)
                .Select(c => new { id = c.CommuneID, name = c.CommuneName })
                .ToListAsync();

            return Json(communes);
        }

        [HttpGet]
        public IActionResult Search(int? ProvinceId, int? DistrictId, int? CommuneId, int? LoaiHinh, int? Gia)
        {
            // Giữ lại dữ liệu để hiển thị lại dropdown
            ViewBag.Provinces = _context.Provinces.OrderBy(p => p.ProvinceName).ToList();
            ViewBag.Categories = _context.Categories.OrderBy(c => c.CategoryName).ToList();

            // ✅ Load danh sách Quận theo Tỉnh (nếu có chọn)
            if (ProvinceId.HasValue)
            {
                ViewBag.Districts = _context.Districts
                    .Where(d => d.ProvinceID == ProvinceId)
                    .OrderBy(d => d.DistrictName)
                    .ToList();
            }

            // ✅ Load danh sách Xã theo Quận (nếu có chọn)
            if (DistrictId.HasValue)
            {
                ViewBag.Communes = _context.CommuneWards
                    .Where(c => c.DistrictID == DistrictId)
                    .OrderBy(c => c.CommuneName)
                    .ToList();
            }

            // ✅ Truyền thêm giá trị đã chọn để View hiển thị lại
            ViewBag.SelectedProvinceId = ProvinceId;
            ViewBag.SelectedDistrictId = DistrictId;
            ViewBag.SelectedCommuneId = CommuneId;
            ViewBag.SelectedLoaiHinh = LoaiHinh;
            ViewBag.SelectedGia = Gia;

            // --- Truy vấn bài đăng ---
            var postsQuery = _context.Posts
                .Where(p => p.Status == "Đã duyệt")
                .Include(p => p.Images.Take(1))
                .Include(p => p.User)
                .Include(p => p.CommuneWard.District.Province)
                .AsQueryable();

            // --- Truy vấn dự án ---
            var projectsQuery = _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .Include(p => p.Images.Take(1))
                .Include(p => p.User)
                .Include(p => p.CommuneWard.District.Province)
                .AsQueryable();

            // --- Áp dụng điều kiện lọc ---
            if (ProvinceId.HasValue)
            {
                postsQuery = postsQuery.Where(p => p.CommuneWard.District.ProvinceID == ProvinceId);
                projectsQuery = projectsQuery.Where(p => p.CommuneWard.District.ProvinceID == ProvinceId);
            }

            if (DistrictId.HasValue)
            {
                postsQuery = postsQuery.Where(p => p.CommuneWard.DistrictID == DistrictId);
                projectsQuery = projectsQuery.Where(p => p.CommuneWard.DistrictID == DistrictId);
            }

            if (CommuneId.HasValue)
            {
                postsQuery = postsQuery.Where(p => p.CommuneID == CommuneId);
                projectsQuery = projectsQuery.Where(p => p.CommuneID == CommuneId);
            }

            if (LoaiHinh.HasValue)
            {
                postsQuery = postsQuery.Where(p => p.CategoryID == LoaiHinh);
            }

            if (Gia.HasValue)
            {
                switch (Gia)
                {
                    case 1:
                        postsQuery = postsQuery.Where(p => p.Price < 1_000_000_000); // < 1 tỷ
                        break;
                    case 2:
                        postsQuery = postsQuery.Where(p => p.Price >= 1_000_000_000 && p.Price < 3_000_000_000);
                        break;
                    case 3:
                        postsQuery = postsQuery.Where(p => p.Price >= 3_000_000_000 && p.Price < 5_000_000_000);
                        break;
                    case 4:
                        postsQuery = postsQuery.Where(p => p.Price >= 5_000_000_000 && p.Price < 10_000_000_000);
                        break;
                    case 5:
                        postsQuery = postsQuery.Where(p => p.Price >= 10_000_000_000);
                        break;
                }
            }

            // --- Lấy danh sách kết quả ---
            var resultPosts = postsQuery.ToList();
            var resultProjects = projectsQuery.ToList();

            // ❤️ Lấy danh sách bài user đã yêu thích (để hiển thị tim đỏ)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<int> favoriteIds = new();
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                favoriteIds = _context.Prefereds
                    .Where(p => p.UserID == userId && p.PostID != null)
                    .Select(p => p.PostID!.Value)
                    .ToList();
            }
            ViewBag.FavoritePostIds = favoriteIds;

            // --- Trả kết quả ---
            ViewBag.ResultPosts = resultPosts;
            ViewBag.ResultProjects = resultProjects;

            return View("Search");
        }


    }
}

