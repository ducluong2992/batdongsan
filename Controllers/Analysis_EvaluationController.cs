using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bds.Models; 
using System.Linq;
using System.Threading.Tasks;
using bds.Data;
using System.Security.Claims;
using System.Text.Json;

namespace bds.Controllers
{
    public class Analysis_EvaluationController : Controller
    {
        private readonly ApplicationDbContext _context;
        public Analysis_EvaluationController(ApplicationDbContext context)
        {
            _context = context;
        }
        // ==========================
        public async Task<IActionResult> Index()
        {
            var provinces = await _context.Provinces
                .OrderBy(p => p.ProvinceName)
                .ToListAsync();

            ViewBag.Provinces = provinces;
            return View();
        }

        // ==========================
        // 1.Xu hướng mua bán (Post + Project)
        // ==========================
        [HttpGet]
        public async Task<JsonResult> GetPostAndProjectTrend()
        {
            // Lấy số lượng Post theo tỉnh
            var postTrend = await _context.Posts
                .Where(p => p.CommuneWard != null && p.CommuneWard.District != null && p.CommuneWard.District.Province != null)
                .GroupBy(p => p.CommuneWard.District.Province.ProvinceName)
                .Select(g => new
                {
                    Province = g.Key,
                    PostCount = g.Count()
                })
                .ToListAsync();

            // Lấy số lượng Project theo tỉnh
            var projectTrend = await _context.Projects
                .Where(p => p.CommuneWard != null && p.CommuneWard.District != null && p.CommuneWard.District.Province != null)
                .GroupBy(p => p.CommuneWard.District.Province.ProvinceName)
                .Select(g => new
                {
                    Province = g.Key,
                    ProjectCount = g.Count()
                })
                .ToListAsync();

            // Dùng dictionary để gộp an toàn
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Thêm post counts
            if (postTrend != null)
            {
                foreach (var p in postTrend)
                {
                    if (string.IsNullOrWhiteSpace(p.Province)) continue;
                    if (!map.ContainsKey(p.Province)) map[p.Province] = 0;
                    map[p.Province] += p.PostCount;
                }
            }

            // Thêm project counts
            if (projectTrend != null)
            {
                foreach (var p in projectTrend)
                {
                    if (string.IsNullOrWhiteSpace(p.Province)) continue;
                    if (!map.ContainsKey(p.Province)) map[p.Province] = 0;
                    map[p.Province] += p.ProjectCount;
                }
            }

            // Chuyển sang list, sắp xếp giảm dần và lấy top 10
            var combined = map
                .Select(kv => new { province = kv.Key, totalCount = kv.Value })
                .OrderByDescending(x => x.totalCount)
                .Take(10)
                .ToList();

            return Json(combined);
        }

        // ==========================
        // 2. Xu hướng giá cả (theo thời gian)
        // ==========================
        [HttpGet]
        public async Task<JsonResult> GetPriceTrend(string province = "all")
        {
            var query = _context.Posts
                .Where(p => p.Price != null
                         && p.CommuneWard != null
                         && p.CommuneWard.District != null
                         && p.CommuneWard.District.Province != null);

            if (province.ToLower() != "all")
                query = query.Where(p => p.CommuneWard.District.Province.ProvinceName == province);

            // 🟢 Bước 1: GroupBy và lấy dữ liệu thô từ DB
            var rawData = await query
                .GroupBy(p => new
                {
                    Province = p.CommuneWard.District.Province.ProvinceName,
                    Year = p.CreateAt.Year,
                    Month = p.CreateAt.Month
                })
                .Select(g => new
                {
                    g.Key.Province,
                    g.Key.Year,
                    g.Key.Month,
                    avgPrice = g.Average(p => p.Price)
                })
                .ToListAsync();

            // 🟢 Bước 2: Chuyển sang bộ nhớ để xử lý label (EF không dịch được string.Concat)
            var data = rawData
                .Select(g => new
                {
                    province = g.Province,
                    label = $"{g.Month}/{g.Year}",
                    avgPrice = g.avgPrice
                })
                .OrderBy(x => x.label)
                .ToList();

            return Json(data);
        }



        // ==========================
        //: 3.đánh giá bài đăng
        // ==========================
        [HttpGet]
        public IActionResult GetPostPerformance()
        {
            // 🔹 Lấy ID người dùng hiện tại (chuỗi)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔹 Kiểm tra hợp lệ và chuyển sang int
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { error = "Chưa đăng nhập hoặc ID không hợp lệ" });
            }

            // 1️⃣ Đếm lượt tym cho Post
            var postLikeCounts = _context.Prefereds
                .Where(p => p.PostID != null)
                .GroupBy(p => p.PostID)
                .Select(g => new { PostID = g.Key.Value, LikeCount = g.Count() })
                .ToList();

            // 2️⃣ Đếm lượt tym cho Project
            var projectLikeCounts = _context.Prefereds
                .Where(p => p.ProjectID != null)
                .GroupBy(p => p.ProjectID)
                .Select(g => new { ProjectID = g.Key.Value, LikeCount = g.Count() })
                .ToList();

            // 3️⃣ Chỉ lấy bài và dự án của user hiện tại
            var posts = _context.Posts
                .Where(p => p.UserID == userId)
                .Select(p => new
                {
                    p.PostID,
                    Name = p.Title,
                    Category = "Post",
                    p.ClickCount,
                    p.CreateAt
                })
                .ToList();

            var projects = _context.Projects
                .Where(pr => pr.UserID == userId)
                .Select(pr => new
                {
                    pr.ProjectID,
                    Name = pr.ProjectName,
                    Category = "Project",
                    pr.ClickCount,
                    pr.CreateAt
                })
                .ToList();

            // 4️⃣ Ghép dữ liệu
            var postData = posts.Select(p => new
            {
                p.Name,
                p.Category,
                p.ClickCount,
                LikeCount = postLikeCounts.FirstOrDefault(x => x.PostID == p.PostID)?.LikeCount ?? 0,
                p.CreateAt
            });

            var projectData = projects.Select(pr => new
            {
                pr.Name,
                pr.Category,
                pr.ClickCount,
                LikeCount = projectLikeCounts.FirstOrDefault(x => x.ProjectID == pr.ProjectID)?.LikeCount ?? 0,
                pr.CreateAt
            });

            // 5️⃣ Hợp & trả về
            var combined = postData.Union(projectData)
                .OrderByDescending(x => x.ClickCount)
                .Select(x => new
                {
                    x.Name,
                    x.Category,
                    x.ClickCount,
                    x.LikeCount,
                    CreatedAt = x.CreateAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(combined, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

    }
}

