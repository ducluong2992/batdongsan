using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace bds.Controllers
{
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProjectController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 1. TRANG DANH SÁCH DỰ ÁN ---
        public async Task<IActionResult> Index()
        {
            // Lấy các dự án nổi bật (Top 5 click, đã duyệt)
            var featuredProjects = await _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.ClickCount)
                .Take(5)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province) // ✅ cập nhật theo mô hình mới
                .ToListAsync();

            ViewBag.FeaturedProjects = featuredProjects;

            // Lấy tất cả dự án (đã duyệt)
            var allProjects = await _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreateAt)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province) // ✅ cập nhật
                .ToListAsync();

            return View(allProjects);
        }

        // --- 2. TRANG CHI TIẾT DỰ ÁN ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // ✅ Lấy các dự án nổi bật (để hiển thị bên phải)
            ViewBag.FeaturedProjects = await _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.ClickCount)
                .Take(5)
                .Include(p => p.Images)
                .Include(p => p.CommuneWard.District.Province)
                .ToListAsync();

            var project = await _context.Projects
                .Include(p => p.User)
                .Include(p => p.CommuneWard.District.Province) // ✅ cập nhật theo quan hệ mới
                .Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null || project.Status != "Đã duyệt")
                return NotFound();

            // --- Đếm lượt click ---
            project.ClickCount = (project.ClickCount ?? 0) + 1;
            _context.Update(project);
            await _context.SaveChangesAsync();

            return View(project);
        }

        // --- 3. TRANG ĐĂNG DỰ ÁN (USER ĐÃ ĐĂNG NHẬP) ---
        [Authorize]
        public IActionResult Create()
        {
            ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
            return View();
        }

        // --- 4. POST: ĐĂNG DỰ ÁN ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectName,Description,Location,Area,StartDate,EndDate,CommuneID")] Project project,
            List<IFormFile>? images)
        {
            // ✅ Lấy ID người dùng từ Claim
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
                return RedirectToAction("Login", "Account");

            // Gán UserID
            project.UserID = int.Parse(currentUserId);
            if (!ModelState.IsValid)
            {
                ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
                return View(project);
            }
            else
            {
                // --- B1: Thêm dự án ---
                project.Status = "Chờ duyệt";
                project.CreateAt = DateTime.Now;
                project.ClickCount = 0;

                _context.Projects.Add(project);
                await _context.SaveChangesAsync(); // ⚡ Lưu để có ProjectID

                // --- B2: Upload và lưu thông tin ảnh ---
                if (images != null && images.Count > 0)
                {
                    string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "projects");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    foreach (var imageFile in images)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string filePath = Path.Combine(uploadDir, uniqueFileName);

                        // Lưu file vào thư mục vật lý
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // --- Ghi bản ghi ảnh vào bảng Images ---
                        var image = new Image
                        {
                            ImageUrl = "/images/projects/" + uniqueFileName,
                            ProjectID = project.ProjectID
                        };
                        _context.Images.Add(image);
                    }

                    // Lưu tất cả ảnh vào DB
                    await _context.SaveChangesAsync();
                }

                // --- B3: Thông báo thành công ---
                TempData["SuccessMessage"] = "Dự án của bạn đã được gửi thành công! Hãy chờ quản trị viên duyệt nhé.";

                return RedirectToAction("Create"); // Quay lại form và hiển thị alert
            }

            //// --- Nếu ModelState không hợp lệ ---
            //ViewData["CommuneID"] = new SelectList(_context.CommuneWards, "CommuneID", "CommuneName", project.CommuneID);
            //return View(project);
        }




        // --- 5. API: LẤY QUẬN/HUYỆN THEO TỈNH ---
        [HttpGet]
        public async Task<JsonResult> GetDistrictsByProvinceId(int provinceId)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceID == provinceId)
                .Select(d => new { id = d.DistrictID, name = d.DistrictName })
                .ToListAsync();

            return Json(districts);
        }

        // --- 6. API: LẤY XÃ/PHƯỜNG THEO QUẬN ---
        [HttpGet]
        public async Task<JsonResult> GetCommunesByDistrictId(int districtId)
        {
            var communes = await _context.CommuneWards
                .Where(c => c.DistrictID == districtId)
                .Select(c => new { id = c.CommuneID, name = c.CommuneName })
                .ToListAsync();

            return Json(communes);
        }

        //// --- 7. THÔNG BÁO ĐĂNG THÀNH CÔNG ---
        //public IActionResult CreateSuccess()
        //{
        //    TempData["SuccessMessage"] = "Dự án của bạn đã được gửi thành công! Hãy chờ quản trị viên duyệt nhé.";
        //    return RedirectToAction("Create");

        //}

    }
}
