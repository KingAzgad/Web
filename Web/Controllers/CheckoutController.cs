using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Cart> GetCart()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { UserId = user.Id };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetCart();
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart"); // Nếu giỏ hàng trống, quay lại trang Cart
            }

            var viewModel = new CheckoutViewModel
            {
                TotalAmount = cart.GetTotal()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetCart();
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart"); // Nếu giỏ hàng trống, quay lại trang Cart
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = cart.UserId,
                OrderDate = DateTime.Now,
                TotalAmount = cart.GetTotal(),
                Status = "Completed", // Thiết lập trạng thái "Đã thanh toán"
                Items = new List<OrderItem>()
            };

            // Chuyển các CartItem thành OrderItem
            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    ProductPrice = cartItem.ProductPrice,
                    ProductImageUrl = cartItem.ProductImageUrl,
                    Quantity = cartItem.Quantity
                };
                order.Items.Add(orderItem);
            }

            // Lưu đơn hàng vào database
            _context.Orders.Add(order);

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cart.Items);
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();

            // Chuyển hướng đến trang xác nhận
            return RedirectToAction("Confirmation", new { orderId = order.Id });
        }

        public async Task<IActionResult> Confirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == _userManager.GetUserId(User));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }

    public class CheckoutViewModel
    {
        public decimal TotalAmount { get; set; }
    }
}