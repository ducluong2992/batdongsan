using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bds.Data;
using bds.Models;
using bds.Services;
using Microsoft.AspNetCore.Authentication;
using System.Linq;
using System.Threading.Tasks;

namespace bds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserMngController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;

        public UserMngController(ApplicationDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        private bool IsSuperAdmin()
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            return currentUser != null && currentUser.IsSuperAdmin;
        }

        public IActionResult Index()
        {
            var users = _context.Users.Where(u => u.RoleID == 2).OrderByDescending(u => u.CreateAt).ToList();
            var admins = _context.Users.Where(u => u.RoleID == 1).OrderByDescending(u => u.IsSuperAdmin).ToList();

            ViewBag.UserCount = users.Count;
            ViewBag.AdminCount = admins.Count;
            ViewBag.IsSuperAdmin = IsSuperAdmin();

            var model = Tuple.Create(users, admins);
            return View(model);
        }

        // Thêm Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAdmin(string username, string password, string email, string fullName, string phone)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction(nameof(Index));
            }

            if (_context.Users.Any(u => u.Username == username))
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email)
                    {
                        TempData["Error"] = "Email không hợp lệ.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch
                {
                    TempData["Error"] = "Email không hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                if (_context.Users.Any(u => u.Email == email))
                {
                    TempData["Error"] = "Email đã được sử dụng.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var admin = new User
            {
                Username = username,
                Password = password,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                FullName = fullName,
                Phone = phone,
                RoleID = 1,
                CreateAt = DateTime.Now
            };

            _context.Users.Add(admin);
            _context.SaveChanges();

            // Add log
            _logService.AddLog(currentUser?.UserID, "AddAdmin", $"{currentUser?.Username} đã thêm quản trị viên: {admin.Username}", "Users", admin.UserID);

            TempData["Success"] = "Thêm quản trị viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Sửa Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(User model)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.UserID == model.UserID);
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (existingUser == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            if (existingUser.RoleID == 1 && !IsSuperAdmin())
            {
                TempData["Error"] = "Bạn không có quyền sửa tài khoản Admin.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(model.Email);
                    if (addr.Address != model.Email)
                    {
                        TempData["Error"] = "Email không hợp lệ.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch
                {
                    TempData["Error"] = "Email không hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                bool emailExists = _context.Users.Any(u => u.UserID != model.UserID && u.Email == model.Email);
                if (emailExists)
                {
                    TempData["Error"] = "Email đã được sử dụng.";
                    return RedirectToAction(nameof(Index));
                }
            }

            existingUser.FullName = model.FullName;
            existingUser.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email;
            existingUser.Phone = model.Phone;

            _context.SaveChanges();

            // Add log
            _logService.AddLog(currentUser?.UserID, "UpdateUser", $"{currentUser?.Username} đã cập nhật thông tin người dùng: {existingUser.Username}", "Users", existingUser.UserID);

            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Khóa / Mở khóa tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockUser(int id)
        {
            var user = _context.Users.Find(id);
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            if (user.RoleID == 1 && !IsSuperAdmin())
            {
                TempData["Error"] = "Bạn không có quyền khóa tài khoản Admin.";
                return RedirectToAction(nameof(Index));
            }

            user.IsLocked = !user.IsLocked;
            _context.Update(user);
            _context.SaveChanges();

            // Add log
            string action = user.IsLocked ? "khóa" : "mở khóa";
            _logService.AddLog(currentUser?.UserID, "ToggleLockUser", $"{currentUser?.Username} đã {action} tài khoản: {user.Username}", "Users", user.UserID);

            if (user.IsLocked && User.Identity.Name == user.Username)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = user.IsLocked ? "Tài khoản đã bị khóa." : "Tài khoản đã được mở khóa.";
            return RedirectToAction(nameof(Index));
        }

        // Tìm kiếm User
        public IActionResult SearchUsers(string keyword)
        {
            var users = _context.Users.Where(u => u.RoleID == 2);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(u => u.Username.Contains(keyword) || u.FullName.Contains(keyword));
            }

            users = users.OrderByDescending(u => u.CreateAt);
            return PartialView("_UserTable", users.ToList());
        }

        // Tìm kiếm Admin
        public IActionResult SearchAdmins(string keyword)
        {
            var admins = _context.Users.Where(u => u.RoleID == 1);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                admins = admins.Where(u => u.Username.Contains(keyword) || u.FullName.Contains(keyword));
            }

            admins = admins.OrderByDescending(u => u.IsSuperAdmin);

            ViewBag.IsSuperAdmin = IsSuperAdmin();
            return PartialView("_AdminTable", admins.ToList());
        }
    }
}
