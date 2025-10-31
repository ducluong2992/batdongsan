using Microsoft.AspNetCore.Mvc;
using bds.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using bds.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using bds.Services;

namespace bds.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;

        public AccountController(ApplicationDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        //================== LOGIN ==================//
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl)
        {
            var user = _context.Users.Include(u => u.Role)
                                     .FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                _logService.AddLog(null, "Login", $"Đăng nhập thất bại với username: {username}", "Users", null, false);
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            if (user.IsLocked)
            {
                _logService.AddLog(user.UserID, "Login", $"{user.Username} đăng nhập thất bại - tài khoản bị khóa", "Users", user.UserID, false);
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            _logService.AddLog(user.UserID, "Login", $"{user.Username} đăng nhập thành công", "Users", user.UserID);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role?.RoleName == "Admin" ? RedirectToAction("Index", "Admin")
                                                  : RedirectToAction("Index", "Home");
        }

        //================== LOGOUT ==================//
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.Identity?.Name ?? "Unknown";
            _logService.AddLog(userId != null ? int.Parse(userId) : null, "Logout", $"{username} đã đăng xuất", "Users", userId != null ? int.Parse(userId) : null);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return RedirectToAction("Index", "Home");
        }

        //================== REGISTER ==================//
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Register model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(u => u.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                _logService.AddLog(null, "Register", $"Đăng ký thất bại - username trùng: {model.Username}", "Users", null, false);
                return View(model);
            }

            var defaultRole = _context.Roles.FirstOrDefault(r => r.RoleName == "User");

            var newUser = new User
            {
                Username = model.Username,
                Password = model.Password, // Nên mã hóa mật khẩu trước khi lưu
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.PhoneNumber,
                RoleID = defaultRole?.RoleID
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            _logService.AddLog(newUser.UserID, "Register", $"{newUser.Username} đã đăng ký thành công", "Users", newUser.UserID);

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        //================== PROFILE ==================//
        public IActionResult Profile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Include(u => u.Role)
                                     .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";
            return View(user);
        }

        //================== EDIT PROFILE ==================//
        [HttpGet]
        public IActionResult EditProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Include(u => u.Role)
                                     .FirstOrDefault(u => u.Username == username);

            if (user == null) return NotFound();
            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User model)
        {
            if (!ModelState.IsValid)
            {
                var failedUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == model.UserID);
                ViewData["Layout"] = failedUser?.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

                _logService.AddLog(model.UserID, "EditProfile", $"{model.Username} cập nhật thông tin thất bại", "Users", model.UserID, false);
                return View(model);
            }

            var existingUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == model.UserID);
            if (existingUser == null) return NotFound();

            existingUser.FullName = model.FullName;
            existingUser.Email = model.Email;
            existingUser.Phone = model.Phone;

            _context.Update(existingUser);
            await _context.SaveChangesAsync();

            _logService.AddLog(existingUser.UserID, "EditProfile", $"{existingUser.Username} cập nhật thông tin thành công", "Users", existingUser.UserID);

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }

        //================== EDIT PASSWORD ==================//
        [HttpGet]
        public IActionResult EditPassword()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                TempData["PasswordError"] = "Không tìm thấy tài khoản.";
                return View();
            }

            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            if (user.Password != OldPassword)
            {
                _logService.AddLog(user.UserID, "EditPassword", $"{user.Username} đổi mật khẩu thất bại - mật khẩu cũ không đúng", "Users", user.UserID, false);
                ViewBag.ErrorOld = "Mật khẩu cũ không đúng.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                _logService.AddLog(user.UserID, "EditPassword", $"{user.Username} đổi mật khẩu thất bại - mật khẩu xác nhận không khớp", "Users", user.UserID, false);
                ViewBag.ErrorConfirm = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            user.Password = NewPassword;
            _context.SaveChanges();

            _logService.AddLog(user.UserID, "EditPassword", $"{user.Username} đổi mật khẩu thành công", "Users", user.UserID);
            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("EditPassword");
        }

        //================== AJAX CHECK OLD PASSWORD ==================//
        [HttpPost]
        public IActionResult CheckOldPassword(string oldPassword)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });

            bool isMatch = user.Password == oldPassword;
            return Json(new { success = isMatch });
        }
    }
}
