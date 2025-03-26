using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Reponsitory;

namespace Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IProductRepository _productRepository;

        public CheckoutController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        public IActionResult ProcessPayment(int productId)
        {
            // Đây là demo, chỉ hiển thị thông báo và chuyển hướng về trang Home
            TempData["Message"] = "Thanh toán thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}