using Microsoft.AspNetCore.Mvc;
using bds.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using bds.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace bds.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context) { 
        _context = context;
        }
        //Dang nhap
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl)  { 
        var user = _context.Users.Include(u=>u.Role).FirstOrDefault(u=>u.Username==username&&u.Password==password);
            if (user == null) {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }
            // Gắm Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };


            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Chuyen huong theo Role 
            if (user.Role?.RoleName == "Admin")
                return RedirectToAction("Index", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        //--dki----
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
            {
                return View(model);
            }

            // Kiểm tra username trùng (không phân biệt hoa/thường)
            if (_context.Users.Any(u => u.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            var defaultRole = _context.Roles.FirstOrDefault(r => r.RoleName == "User");

            var newUser = new User
            {
                Username = model.Username,
                Password = model.Password, // nên mã hóa trước khi lưu (bcrypt/SHA256)
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.PhoneNumber,
                RoleID = defaultRole?.RoleID
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }


        // Dang xuat
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return RedirectToAction("Index", "Home");
        }


        //truy cap trai phep
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult Index()
        {
            return View();
        }

        // PROFILE
        public IActionResult Profile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            return View(user);
        }

        // GET: EditProfile
        [HttpGet]
        public IActionResult EditProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");
            var user = _context.Users
                               .Include(u => u.Role)
                               .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return NotFound();
            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            return View(user);
        }

        // POST: EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User model)
        {
            if (!ModelState.IsValid)
            {
                var failedUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == model.UserID);

                ViewData["Layout"] = failedUser?.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";
                return View(model);
            }

            var existingUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == model.UserID);

            if (existingUser == null)
                return NotFound();

            existingUser.FullName = model.FullName;
            existingUser.Email = model.Email;
            existingUser.Phone = model.Phone;

            _context.Update(existingUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";

            return RedirectToAction(nameof(Profile));
        }

        // PASSWORD
        // GET: EditPassword
        [HttpGet]
        public IActionResult EditPassword()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Include(u => u.Role)
                        .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            return View();
        }

        // POST: EditPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Include(u => u.Role)
                        .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                TempData["PasswordError"] = "Không tìm thấy tài khoản.";
                return View();
            }

            ViewData["Layout"] = user.Role?.RoleName == "Admin" ? "_LayoutAdmin" : "_Layout";

            if (user.Password != OldPassword)
            {
                ViewBag.ErrorOld = "Mật khẩu cũ không đúng.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.ErrorConfirm = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            user.Password = NewPassword;
            _context.SaveChanges();

            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("EditPassword");
        }

        // AJAX: Check mật khẩu cũ
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
