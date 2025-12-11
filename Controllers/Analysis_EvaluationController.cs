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
                .Where(p => p.Status != "Không duyệt" && p.CommuneWard != null && p.CommuneWard.District != null && p.CommuneWard.District.Province != null)
                .GroupBy(p => p.CommuneWard.District.Province.ProvinceName)
                .Select(g => new
                {
                    Province = g.Key,
                    PostCount = g.Count()
                })
                .ToListAsync();

            // Lấy số lượng Project theo tỉnh
            var projectTrend = await _context.Projects
                .Where(p => p.Status != "Không duyệt" && p.CommuneWard != null && p.CommuneWard.District != null && p.CommuneWard.District.Province != null)
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
                .Where(p => p.Status != "Không duyệt"
                         &&p.Price != null
                         && p.CommuneWard != null
                         && p.CommuneWard.District != null
                         && p.CommuneWard.District.Province != null);

            if (province.ToLower() != "all")
                query = query.Where(p => p.CommuneWard.District.Province.ProvinceName == province);

            // 🟢 GroupBy theo tỉnh + năm + tháng
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
                    AvgPrice = g.Average(p => p.Price)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            // 🟢 Tạo label sau khi đã sắp đúng thứ tự
            var data = rawData.Select(g => new
            {
                province = g.Province,
                label = $"{g.Month:D2}/{g.Year}", // thêm D2 để 01, 02, 03... giúp JS sort đúng
                avgPrice = Math.Round(g.AvgPrice ?? 0, 2)
            }).ToList();

            return Json(data);
        }



        // ==========================
        //: 3.đánh giá bài đăng
        // ==========================
        [HttpGet]
        public IActionResult GetPostPerformance()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { error = "Chưa đăng nhập hoặc ID không hợp lệ" });

            // ===== Lấy dữ liệu yêu thích (tym) trước ====
            var postLikeCounts = _context.Prefereds
                .Where(p => p.PostID != null)
                .GroupBy(p => p.PostID)
                .Select(g => new { PostID = g.Key.Value, LikeCount = g.Count() })
                .ToList();

            var projectLikeCounts = _context.Prefereds
                .Where(p => p.ProjectID != null)
                .GroupBy(p => p.ProjectID)
                .Select(g => new { ProjectID = g.Key.Value, LikeCount = g.Count() })
                .ToList();

            // 🔹 Cập nhật trạng thái hết hạn trước khi trả dữ liệu
            var now = DateTime.Now;
            var expiredPosts = _context.Posts
                .Where(p => p.Status != "Hết hạn" && now > p.CreateAt.AddDays(20))
                .ToList();

            foreach (var p in expiredPosts)
                p.Status = "Hết hạn";

            var expiredProjects = _context.Projects
                .Where(pr => pr.Status != "Hết hạn" && now > pr.CreateAt.AddDays(20))
                .ToList();

            foreach (var pr in expiredProjects)
                pr.Status = "Hết hạn";

            if (expiredPosts.Any() || expiredProjects.Any())
                _context.SaveChanges();


            // ===== Lấy danh sách bài đăng của user ====
            var posts = _context.Posts
                .Where(p => p.UserID == userId && p.Status != "Không duyệt")
                .Select(p => new
                {
                    ID = p.PostID,
                    Name = p.Title,
                    Category = "Post",
                    p.ClickCount,
                    p.CreateAt
                })
                .ToList();

            // ===== Lấy danh sách dự án của user ====
            var projects = _context.Projects
                .Where(pr => pr.UserID == userId && pr.Status != "Không duyệt")
                .Select(pr => new
                {
                    ID = pr.ProjectID,
                    Name = pr.ProjectName,
                    Category = "Project",
                    pr.ClickCount,
                    pr.CreateAt
                })
                .ToList();

            // ===== Ghép LikeCount (xử lý trong bộ nhớ C#) ====
            var postData = posts.Select(p => new
            {
                p.ID,
                p.Name,
                p.Category,
                p.ClickCount,
                LikeCount = postLikeCounts
                    .Where(x => x.PostID == p.ID)
                    .Select(x => x.LikeCount)
                    .FirstOrDefault(),
                CreatedAt = p.CreateAt,
                ExpireAt = p.CreateAt.AddDays(20)
            });

            var projectData = projects.Select(pr => new
            {
                pr.ID,
                pr.Name,
                pr.Category,
                pr.ClickCount,
                LikeCount = projectLikeCounts
                    .Where(x => x.ProjectID == pr.ID)
                    .Select(x => x.LikeCount)
                    .FirstOrDefault(),
                CreatedAt = pr.CreateAt,
                ExpireAt = pr.CreateAt.AddDays(20)
            });

            // ===== Gộp dữ liệu (Concat) ====
            var combined = postData
                .Concat(projectData)
                .OrderByDescending(x => x.ClickCount)
                .Select(x => new
                {
                    x.Name,
                    x.Category,
                    x.ClickCount,
                    x.LikeCount,
                    createdAt = x.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    expireAt = x.ExpireAt.ToString("yyyy-MM-ddTHH:mm:ss")
                })
                .ToList();

            return Json(combined, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

    }
}

