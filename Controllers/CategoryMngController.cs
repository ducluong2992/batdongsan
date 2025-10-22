using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bds.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryMngController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryMngController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            ViewBag.NewCategory = new Category();
            return View(categories);
        }
//them danh muc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Category model)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.Categories.Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower());

                if (exists){TempData["Error"] = "Tên danh mục đã tồn tại!";}
                else {
                    _context.Categories.Add(model);
                    _context.SaveChanges();
                    TempData["Success"] = "Thêm danh mục thành công!";
                }
            }
            else {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
            }

            return RedirectToAction(nameof(Index));
        }

        //sua danh muc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                TempData["Error"] = "Vui lòng nhập tên danh mục!";
            }
            else
            {
                bool nameExists = _context.Categories.Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower()&& c.CategoryID != model.CategoryID);

                if (nameExists)
                {
                    TempData["Error"] = "Tên danh mục đã tồn tại!";
                }
                else
                {
                    var existingCategory = _context.Categories.First(c => c.CategoryID == model.CategoryID);
                    existingCategory.CategoryName = model.CategoryName.Trim();
                    _context.SaveChanges();
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                }
            }

            return RedirectToAction(nameof(Index));
        }



        // xoa danh muc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy danh mục cần xóa!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
