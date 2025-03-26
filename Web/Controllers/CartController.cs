using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Models;
using Web.Reponsitory;
using Microsoft.AspNetCore.Identity;

namespace Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(IProductRepository productRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
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

            // Thêm log để debug
            Console.WriteLine($"Cart ID: {cart.Id}, Số lượng sản phẩm: {cart.Items?.Count ?? 0}");
            if (cart.Items != null)
            {
                foreach (var item in cart.Items)
                {
                    Console.WriteLine($"Sản phẩm: {item.ProductName}, Số lượng: {item.Quantity}");
                }
            }
            else
            {
                Console.WriteLine("Items is null");
            }

            return cart;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = await GetCart();

            // Đảm bảo cart đã được lưu vào database
            if (cart.Id == 0)
            {
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductPrice = product.Price,
                    ProductImageUrl = product.ImageUrl,
                    Quantity = quantity,
                    CartId = cart.Id
                };
                cart.Items.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetCart();
            return View(cart);
        }

        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var cart = await GetCart();
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cart.Items.Remove(cartItem);
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}