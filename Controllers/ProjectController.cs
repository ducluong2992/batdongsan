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
                .Include(p => p.CommuneWard.District.Province) // cập nhật theo mô hình mới
                .ToListAsync();

            ViewBag.FeaturedProjects = featuredProjects;

            // hiển thị tym 
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<int> favoriteIds = new();

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                favoriteIds = await _context.Prefereds
                    .Where(p => p.UserID == userId && p.ProjectID != null)
                    .Select(p => p.ProjectID!.Value)
                    .ToListAsync();
            }

            ViewBag.FavoriteProjectIds = favoriteIds;

            // Lấy tất cả dự án (đã duyệt)
            var allProjects = await _context.Projects
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreateAt)
                .Include(p => p.Images.Take(1))
                .Include(p => p.CommuneWard.District.Province) // cập nhật
                .ToListAsync();

            //dùng để lọc
            ViewBag.Provinces = _context.Provinces.OrderBy(p => p.ProvinceName).ToList();


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
                .Include(p => p.CommuneWard.District.Province) //cập nhật theo quan hệ mới
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
            // Lấy ID người dùng từ Claim
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
                await _context.SaveChangesAsync(); // Lưu để có ProjectID

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

        // --- 7. Quản lý Dự án của tôi ---
        public async Task<IActionResult> MyProjects()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var myProjects = await _context.Projects
                .Where(p => p.UserID == currentUserId)
                .Include(p => p.Images)
                .Include(p => p.CommuneWard).ThenInclude(c => c.District)
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return View(myProjects);
        }

        // --- 7.1. Xem chi tiết dự án của tôi ----
        public async Task<IActionResult> MyProjectDetails(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Images)
                .Include(p => p.CommuneWard).ThenInclude(c => c.District).ThenInclude(d => d.Province)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // GET: /Project/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects
                .Include(p => p.CommuneWard)
                    .ThenInclude(c => c.District)
                        .ThenInclude(d => d.Province)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null)
                return NotFound();

            ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");

            ViewBag.ProvinceID = project.CommuneWard?.District?.ProvinceID;
            ViewBag.DistrictID = project.CommuneWard?.DistrictID;
            ViewBag.CommuneID = project.CommuneID;

            return View(project);
        }


        // == 8. EDIT ===============
        // POST: /Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName,Description,Location,Area,StartDate,EndDate,CommuneID")] Project project, List<IFormFile>? images)
        {
            if (id != project.ProjectID)
                return NotFound();

            var existingProject = await _context.Projects
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (existingProject == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // reload ViewBag nếu cần rồi trả về View
                ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
                return View(project);
            }

            // --- CẬP NHẬT TRƯỜNG CƠ BẢN ---
            bool isModified =
                existingProject.ProjectName != project.ProjectName ||
                existingProject.Description != project.Description ||
                existingProject.Location != project.Location ||
                existingProject.Area != project.Area ||
                existingProject.StartDate != project.StartDate ||
                existingProject.EndDate != project.EndDate ||
                existingProject.CommuneID != project.CommuneID;

            // Cập nhật dữ liệu mới
            existingProject.ProjectName = project.ProjectName;
            existingProject.Description = project.Description;
            existingProject.Location = project.Location;
            existingProject.Area = project.Area;
            existingProject.StartDate = project.StartDate;
            existingProject.EndDate = project.EndDate;
            existingProject.CommuneID = project.CommuneID;

            // Kiểm tra có thay đổi ảnh không
            bool imagesChanged = (images != null && images.Count > 0);

            // Nếu có thay đổi -> chuyển trạng thái về Chờ duyệt
            if (isModified || imagesChanged)
            {
                existingProject.Status = "Chờ duyệt";
                existingProject.RejectReason = "";
            }

            // Không thay đổi -> giữ nguyên Status cũ
            existingProject.CreateAt = DateTime.Now;


            // --- ẢNH: nếu có ảnh mới -> XÓA ảnh cũ + LƯU ảnh mới
            if (images != null && images.Count > 0)
            {
                // Tạo folder nếu chưa tồn tại
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "projects");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                // XÓA files cũ (trong wwwroot) và xóa record cũ trong _context.Images
                if (existingProject.Images != null && existingProject.Images.Any())
                {
                    foreach (var oldImg in existingProject.Images.ToList())
                    {
                        try
                        {
                            var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, oldImg.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                        catch
                        {
                        }
                    }

                    // RemoveRange hoặc Remove từng item
                    _context.Images.RemoveRange(existingProject.Images);
                    // clear local collection to avoid duplicates
                    existingProject.Images.Clear();
                }

                // Lưu ảnh mới
                foreach (var file in images)
                {
                    if (file.Length <= 0) continue;
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var newImage = new Image
                    {
                        ImageUrl = "/images/projects/" + fileName,
                        // ProjectID will be set by EF when saving (existingProject.ProjectID already set)
                    };
                    existingProject.Images.Add(newImage);
                }
            }
            // else: user did NOT upload new images -> giữ nguyên existingProject.Images

            try
            {
                _context.Update(existingProject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật dự án thành công!";
                return RedirectToAction("MyProjectDetails", new { id = existingProject.ProjectID });
            }
            catch (DbUpdateException dbEx)
            {
                // bạn có thể log dbEx
                TempData["ErrorMessage"] = "Có lỗi khi cập nhật dữ liệu: " + dbEx.Message;
                ViewData["ProvinceList"] = new SelectList(_context.Provinces, "ProvinceID", "ProvinceName");
                return View(project);
            }
        }



        //9. ======================== DELETE ========================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project != null)
            {
                // Xóa ảnh trong thư mục vật lý
                foreach (var img in project.Images)
                {
                    var path = Path.Combine(_webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Dự án đã được xóa thành công!";
            }

            return RedirectToAction("MyProjects");
        }

        // =================10. LỌC ================================
        // Lọc dự án theo tỉnh/quận (trả về partial)
        [HttpGet]
        public async Task<IActionResult> FilterProjects(int? provinceId, int? districtId)
        {
            var query = _context.Projects
                .Include(p => p.Images)
                .Include(p => p.CommuneWard)
                    .ThenInclude(c => c.District)
                        .ThenInclude(d => d.Province)
                .AsQueryable();

            if (provinceId.HasValue)
            {
                query = query.Where(p => p.CommuneWard.District.ProvinceID == provinceId);
            }

            if (districtId.HasValue)
            {
                query = query.Where(p => p.CommuneWard.DistrictID == districtId);
            }

            var projects = await query
                .OrderByDescending(p => p.CreateAt)
                .Select(p => new
                {
                    p.ProjectID,
                    p.ProjectName,
                    p.Description,
                    p.ClickCount,
                    StartDate = p.StartDate.HasValue ? p.StartDate.Value.ToString("dd/MM/yyyy") : "",
                    EndDate = p.EndDate.HasValue ? p.EndDate.Value.ToString("dd/MM/yyyy") : "",
                    ProvinceName = p.CommuneWard.District.Province.ProvinceName,
                    DistrictName = p.CommuneWard.District.DistrictName,
                    CommuneName = p.CommuneWard.CommuneName,
                    ImageUrl = p.Images.FirstOrDefault().ImageUrl
                })
                .ToListAsync();

            return Json(projects);
        }


    }
}
