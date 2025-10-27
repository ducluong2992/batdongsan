using System.Diagnostics;
using bds.Data;
using bds.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace bds.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Load danh sách tỉnh và loại hình (Category)
            ViewBag.Provinces = _context.Provinces
                .OrderBy(p => p.ProvinceName)
                .ToList();

            ViewBag.Categories = _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            return View();
        }

        // --- API load Quận/Huyện theo Tỉnh ---
        [HttpGet]
        public async Task<JsonResult> GetDistrictsByProvinceId(int provinceId)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceID == provinceId)
                .Select(d => new { id = d.DistrictID, name = d.DistrictName })
                .ToListAsync();

            return Json(districts);
        }

        // --- API load Phường/Xã theo Quận ---
        [HttpGet]
        public async Task<JsonResult> GetCommunesByDistrictId(int districtId)
        {
            var communes = await _context.CommuneWards
                .Where(c => c.DistrictID == districtId)
                .Select(c => new { id = c.CommuneID, name = c.CommuneName })
                .ToListAsync();

            return Json(communes);
        }
    }
}

