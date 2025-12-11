using bds.Data;
using bds.Models;
using bds.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace bds.Controllers
{
    public class PostMngController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;

        public PostMngController(ApplicationDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        // ===== Hiển thị danh sách Post và Project chờ duyệt =====
        public async Task<IActionResult> Pending()
        {
            var pendingPosts = await _context.Posts.Include(p => p.User).ToListAsync();
            pendingPosts = pendingPosts.Where(p => p.Status == "Chờ duyệt").ToList();

            var pendingProjects = await _context.Projects.Include(pr => pr.User).ToListAsync();
            pendingProjects = pendingProjects.Where(pr => pr.Status == "Chờ duyệt").ToList();

            var model = new PendingViewModel
            {
                Posts = pendingPosts,
                Projects = pendingProjects
            };

            return View(model);
        }

        // ===== Hiển thị danh sách Post và Project đã duyệt / không duyệt =====
        public async Task<IActionResult> Approved()
        {
            var approvedPosts = await _context.Posts.Include(p => p.User).ToListAsync();
            approvedPosts = approvedPosts.Where(p => p.Status != "Chờ duyệt").ToList();

            var approvedProjects = await _context.Projects.Include(pr => pr.User).ToListAsync();
            approvedProjects = approvedProjects.Where(pr => pr.Status != "Chờ duyệt").ToList();

            var model = new ApprovedViewModel
            {
                Posts = approvedPosts,
                Projects = approvedProjects
            };

            return View(model);
        }

        // ===== Xem chi tiết Post =====
        public async Task<IActionResult> PostDetails(int id)
        {
            var post = await _context.Posts.Include(p => p.User).Include(p => p.Images).FirstOrDefaultAsync(p => p.PostID == id);
            if (post == null) return NotFound();
            return View(post);
        }

        // ===== Xem chi tiết Project =====
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _context.Projects.Include(pr => pr.User).Include(pr => pr.Images).FirstOrDefaultAsync(pr => pr.ProjectID == id);
            if (project == null) return NotFound();
            return View(project);
        }

        // ===== Duyệt Post =====
        [HttpPost]
        public async Task<IActionResult> ApprovePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserID == adminId && u.RoleID == 1);
            
            var superAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.RoleID == 1 && u.IsSuperAdmin);

            if (superAdmin != null)
            {
                superAdmin.Coins += 10;
            }


            post.Status = "Đã duyệt";
            post.RejectReason = null;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho người đăng
            var notification = new Notification
            {
                UserID = post.UserID ?? 0,
                PostID = post.PostID,
                Title = "Bài đăng đã được duyệt",
                Message = $"Bài đăng \"{post.Title}\" của bạn đã được duyệt và hiển thị công khai.",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();


            // Add log
            var username = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logService.AddLog(userId != null ? int.Parse(userId) : (int?)null, "ApprovePost", $"{username} đã duyệt Post: {post.Title}", "Post", post.PostID, true);

            return RedirectToAction(nameof(Pending));
        }

        // ===== Không duyệt Post =====
        [HttpPost]
        public async Task<IActionResult> RejectPost(int id, string reason)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostID == id);


            var user = post.User;
            if (user == null) return NotFound();

            user.Coins += 10;

            post.Status = "Không duyệt";
            post.RejectReason = reason;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho người đăng
            var notification = new Notification
            {
                UserID = post.UserID ?? 0,
                PostID = post.PostID,
                Title = "Bài đăng bị từ chối",
                Message = $"Bài đăng \"{post.Title}\" của bạn đã bị từ chối. Lý do: {reason}",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();


            // Add log
            var username = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logService.AddLog(userId != null ? int.Parse(userId) : (int?)null, "RejectPost", $"{username} đã từ chối Post: {post.Title}. Lý do: {reason}", "Post", post.PostID, true);

            return RedirectToAction(nameof(Pending));
        }

        // ===== Duyệt Project =====
        [HttpPost]
        public async Task<IActionResult> ApproveProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserID == adminId && u.RoleID == 1);


            var superAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.RoleID == 1 && u.IsSuperAdmin);

            if (superAdmin != null)
            {
                superAdmin.Coins += 10;
            }

            project.Status = "Đã duyệt";
            project.RejectReason = null;
            await _context.SaveChangesAsync();

            // ✅ Gửi thông báo cho người đăng dự án
            if (project.UserID != null)
            {
                var notification = new Notification
                {
                    UserID = project.UserID.Value,
                    ProjectID = project.ProjectID, // ✅ thêm dòng này
                    Title = "Dự án đã được duyệt",
                    Message = $"Dự án \"{project.ProjectName}\" của bạn đã được duyệt và hiển thị công khai.",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }


            // Add log
            var username = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logService.AddLog(userId != null ? int.Parse(userId) : (int?)null, "ApproveProject", $"{username} đã duyệt Project: {project.ProjectName}", "Projects", project.ProjectID, true);

            return RedirectToAction(nameof(Pending));
        }

        // ===== Không duyệt Project =====
        [HttpPost]
        public async Task<IActionResult> RejectProject(int id, string reason)
        {
            var project = await _context.Projects
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ProjectID == id);


            var user = project.User;
            if (user == null) return NotFound();

            user.Coins += 10;

            project.Status = "Không duyệt";
            project.RejectReason = reason;
            await _context.SaveChangesAsync();

            // ✅ Gửi thông báo cho người đăng dự án
            if (project.UserID != null)
            {
                var notification = new Notification
                {
                    UserID = project.UserID.Value,
                    ProjectID = project.ProjectID, // ✅ thêm dòng này
                    Title = "Dự án bị từ chối",
                    Message = $"Dự án \"{project.ProjectName}\" của bạn đã bị từ chối. Lý do: {reason}",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }


            // Add log
            var username = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logService.AddLog(userId != null ? int.Parse(userId) : (int?)null, "RejectProject", $"{username} đã từ chối Project: {project.ProjectName}. Lý do: {reason}", "Projects", project.ProjectID, true);

            return RedirectToAction(nameof(Pending));
        }
    }

    public class PendingViewModel
    {
        public List<Post> Posts { get; set; } = new List<Post>();
        public List<Project> Projects { get; set; } = new List<Project>();
    }

    public class ApprovedViewModel
    {
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public IEnumerable<Post> Posts { get; set; } = new List<Post>();
    }
}
