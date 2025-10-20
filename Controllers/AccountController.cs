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
        public IActionResult Login() { 
        return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password) { 
        var user = _context.Users.Include(u=>u.Role).FirstOrDefault(u=>u.Username==username&&u.Password==password);
            if (user == null) {
                ViewBag.Error = "Ten dang nhap hoac mat khau khong dung";
                return View();
            }
        // Gan Claims
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Chuyen huong theo Role 
            if (user.Role?.RoleName == "Admin")
                return RedirectToAction("Index", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        // dang ky
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại.";
                return View();
            }

            var defaultRole = _context.Roles.FirstOrDefault(r => r.RoleName == "User");

            var newUser = new User
            {
                Username = username,
                Password = password,
                Email = email,
                RoleID = defaultRole?.RoleID
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

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
    }
}
