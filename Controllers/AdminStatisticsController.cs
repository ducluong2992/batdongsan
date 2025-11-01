
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bds.Models;
using bds.Data;
using System.Linq;
using System.Threading.Tasks;

namespace bds.Controllers
{
    public class AdminStatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminStatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult ThongKeBaiDang()
        {
            return View("PostStatistics");
        }
        public IActionResult SystemStatistics()
        {
            return View("SystemStatistics");
        }
   public IActionResult Dashboard()
{
    return View("~/Views/Admin/Index.cshtml");
}


        // --- Projects ---
        [HttpGet]
        public async Task<IActionResult> ProjectStatusChart()
        {
            var data = await _context.Projects
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> ProjectMonthlyChart(int year)
        {
            var data = await _context.Projects
                .Where(p => p.Status == "Đã duyệt" && p.CreateAt.Year == year)
                .GroupBy(p => p.CreateAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = Enumerable.Range(1, 12)
                .Select(m => new { Month = m, Count = data.FirstOrDefault(d => d.Month == m)?.Count ?? 0 });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> ProjectProvinceChart()
        {
            var data = await _context.Projects
                .Include(p => p.CommuneWard)
                    .ThenInclude(c => c.District)
                        .ThenInclude(d => d.Province)
                .Where(p => p.Status == "Đã duyệt" && p.CommuneWard != null)
                .Select(p => p.CommuneWard.District.Province.ProvinceName)
                .GroupBy(name => name)
                .Select(g => new { Province = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        // --- Posts ---
        [HttpGet]
        public async Task<IActionResult> PostStatusChart()
        {
            var data = await _context.Post
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> PostMonthlyChart(int year)
        {
            var data = await _context.Post
                .Where(p => p.Status == "Đã duyệt" && p.CreateAt.Year == year)
                .GroupBy(p => p.CreateAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = Enumerable.Range(1, 12)
                .Select(m => new { Month = m, Count = data.FirstOrDefault(d => d.Month == m)?.Count ?? 0 });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> PostProvinceChart()
        {
            var data = await _context.Post
                .Include(p => p.CommuneWard)
                    .ThenInclude(c => c.District)
                        .ThenInclude(d => d.Province)
                .Where(p => p.Status == "Đã duyệt" && p.CommuneWard != null)
                .Select(p => p.CommuneWard.District.Province.ProvinceName)
                .GroupBy(name => name)
                .Select(g => new { Province = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        // --- Log Statistics ---

        // 1. Những Log lỗi nhiều nhất (IsSuccess = false) theo ActionType
        [HttpGet]
        public async Task<IActionResult> TopErrorLogs(int top = 10)
        {
            var data = await _context.Logs
                .Where(l => !l.IsSuccess)
                .GroupBy(l => l.ActionType)
                .Select(g => new
                {
                    ActionType = g.Key,
                    ErrorCount = g.Count()
                })
                .OrderByDescending(x => x.ErrorCount)
                .Take(top)
                .ToListAsync();

            return Json(data);
        }

        // 2. Số người hoạt động trong ngày (UserID != null)
        [HttpGet]
        public async Task<IActionResult> ActiveUsersToday()
        {
            var today = DateTime.Today;
            var count = await _context.Logs
                .Where(l => l.UserID != null && l.CreatedAt.Date == today)
                .Select(l => l.UserID)
                .Distinct()
                .CountAsync();

            return Json(new { ActiveUsers = count });
        }
      
        // 3. Tỉ lệ log thành công / thất bại
        [HttpGet]
        public async Task<IActionResult> LogSuccessRate()
        {
            var total = await _context.Logs.CountAsync();
            var success = await _context.Logs.CountAsync(l => l.IsSuccess);
            var fail = total - success;
            var successRate = total > 0 ? ((double)success / total) * 100 : 0;

            return Json(new { Total = total, Success = success, Fail = fail, SuccessRate = successRate });
        }
        //4.Thống kê số người dùng đăng kí mới 
        [HttpGet]
        public async Task<IActionResult> UserRegisterDailyChart(int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days + 1);

            var data = await _context.Users
                .Where(u => u.CreateAt >= startDate)
                .GroupBy(u => u.CreateAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Bổ sung những ngày không có đăng ký
            var result = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i))
                .Select(d => new
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = data.FirstOrDefault(x => x.Date == d)?.Count ?? 0
                });

            return Json(result);
        }


        // 5. Người dùng hoạt động nhiều nhất
        [HttpGet]
        public async Task<IActionResult> TopActiveUsers(int top = 5)
        {
            var data = await _context.Logs
                .Where(l => l.UserID != null)
                .GroupBy(l => l.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    ActionCount = g.Count()
                })
                .OrderByDescending(x => x.ActionCount)
                .Take(top)
                .Join(
                    _context.Users,            
                    log => log.UserID,            
                    user => user.UserID,
                    (log, user) => new
                    {
                        user.Username,
                        user.FullName,
                        RoleName = user.RoleID == 1 ? "Admin" : "User",
                        log.ActionCount
                    }
                )
                .ToListAsync();

            return Json(data);
        }

        // 6. Danh sách log gần đây nhất (20 log)
        [HttpGet]
        public async Task<IActionResult> RecentLogs(int top = 20)
        {
            var logs = await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(top)
                .Select(l => new
                {
                    l.LogID,
                    Username = l.User != null ? l.User.Username : "—",
                    l.ActionType,
                    l.ActionDescription,
                    l.TableName,
                    l.RecordID,
                    l.IPAddress,
                    l.BrowserInfo,
                    l.IsSuccess,
                    l.CreatedAt
                })
                .ToListAsync();

            return Json(logs);
        }

        //THỐNG KÊ TRANG DASHBOARD
        [HttpGet]
        public async Task<IActionResult> PendingPostsCount()
        {
            var postCount = await _context.Post.CountAsync(p => p.Status == "Chờ duyệt");
            var projectCount = await _context.Projects.CountAsync(p => p.Status == "Chờ duyệt");
            var total = postCount + projectCount;

            return Json(new
            {
                Title = "Bài đăng chờ duyệt",
                Count = total,
                DetailUrl = Url.Action("Pending", "PostMng") 
            });
        }

        [HttpGet]
        public async Task<IActionResult> ApprovedOrRejectedPostsCount()
        {
            var postCount = await _context.Post.CountAsync(p => p.Status == "Đã duyệt" || p.Status == "Không duyệt");
            var projectCount = await _context.Projects.CountAsync(p => p.Status == "Đã duyệt" || p.Status == "Không duyệt");
            var total = postCount + projectCount;

            return Json(new
            {
                Title = "Bài đăng đã duyệt / từ chối",
                Count = total,
                DetailUrl = Url.Action("Approved", "PostMng") 
            });
        }

        [HttpGet]
        public async Task<IActionResult> UserAccountsCount()
        {
            var count = await _context.Users.CountAsync(u => u.RoleID == 2);
            return Json(new
            {
                Title = "Tổng tài khoản người dùng",
                Count = count,
                DetailUrl = Url.Action("Index", "UserMng") 
            });
        }
        [HttpGet]
        public async Task<IActionResult> AveragePriceByProvince()
        {
            var data = await _context.Post
                .Include(p => p.CommuneWard)
                    .ThenInclude(c => c.District)
                        .ThenInclude(d => d.Province)
                .Where(p => p.Status == "Đã duyệt" && p.CommuneWard != null && p.Price > 0 && p.Area > 0)
                .GroupBy(p => p.CommuneWard.District.Province.ProvinceName)
                .Select(g => new
                {
                    Province = g.Key,
                    AveragePrice = Math.Round(
                        g.Sum(p => (double)(p.Price ?? 0)) / g.Sum(p => p.Area ?? 0), 2
                    ) 
                })
                .OrderByDescending(x => x.AveragePrice)
                .ToListAsync();

            return Json(data);
        }
    }
}
