using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web.Models;
using Web.Reponsitory;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;

        public HomeController(
            ILogger<HomeController> logger,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository)
        {
            _logger = logger;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index(int? categoryId, string searchTerm)
        {
            try
            {
                // L?y danh sách t?t c? danh m?c
                var categories = await _categoryRepository.GetAllAsync();

                // L?y danh sách s?n ph?m
                var products = await _productRepository.GetAllAsync();

                // N?u có categoryId, l?c s?n ph?m theo danh m?c
                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
                    ViewBag.SelectedCategoryId = categoryId; // L?u categoryId ?? highlight danh m?c ???c ch?n
                }

                // N?u có searchTerm, l?c s?n ph?m theo t? khóa tìm ki?m
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    products = products.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower())).ToList();
                    ViewBag.SearchTerm = searchTerm; // Truy?n searchTerm ?? hi?n th? trong view
                }

                // Truy?n d? li?u vào ViewBag
                ViewBag.Categories = categories;
                ViewBag.Products = products;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HomeController.Index");
                return View("Error");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}