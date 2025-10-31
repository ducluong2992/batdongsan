using bds.Data;
using bds.Models;
using bds.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace bds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryMngController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _logService;

        public CategoryMngController(ApplicationDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            ViewBag.NewCategory = new Category();
            return View(categories);
        }

        // Thêm danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Category model)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (ModelState.IsValid)
            {
                bool exists = _context.Categories.Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower());

                if (exists)
                {
                    TempData["Error"] = "Tên danh mục đã tồn tại!";
                }
                else
                {
                    _context.Categories.Add(model);
                    _context.SaveChanges();

                    TempData["Success"] = "Thêm danh mục thành công!";
                    _logService.AddLog(currentUser?.UserID, "AddCategory", $"{currentUser?.Username} đã thêm danh mục: {model.CategoryName}", "Categories", model.CategoryID);
                }
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Sửa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Category model)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                TempData["Error"] = "Vui lòng nhập tên danh mục!";
            }
            else
            {
                bool nameExists = _context.Categories.Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower() && c.CategoryID != model.CategoryID);

                if (nameExists)
                {
                    TempData["Error"] = "Tên danh mục đã tồn tại!";
                }
                else
                {
                    var existingCategory = _context.Categories.First(c => c.CategoryID == model.CategoryID);
                    string oldName = existingCategory.CategoryName;

                    existingCategory.CategoryName = model.CategoryName.Trim();
                    _context.SaveChanges();

                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    _logService.AddLog(currentUser?.UserID, "UpdateCategory", $"{currentUser?.Username} đã cập nhật danh mục: {oldName} → {existingCategory.CategoryName}", "Categories", existingCategory.CategoryID);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Xóa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            var category = _context.Categories.Find(id);

            if (category != null)
            {
                string catName = category.CategoryName;
                _context.Categories.Remove(category);
                _context.SaveChanges();

                TempData["Success"] = "Xóa danh mục thành công!";
                _logService.AddLog(currentUser?.UserID, "DeleteCategory", $"{currentUser?.Username} đã xóa danh mục: {catName}", "Categories", id);
            }
            else
            {
                TempData["Error"] = "Không tìm thấy danh mục cần xóa!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
