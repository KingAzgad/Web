using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId, string searchTerm)
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var products = await _productRepository.GetAllAsync();

                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
                    ViewBag.SelectedCategoryId = categoryId;
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    products = products.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower())).ToList();
                    ViewBag.SearchTerm = searchTerm;
                }

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