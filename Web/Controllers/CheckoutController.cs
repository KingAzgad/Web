using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Models;
using Microsoft.AspNetCore.Identity;
using QRCoder;
using System.Drawing;
using System.IO;

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

        // Danh sách ngân hàng
        private List<BankInfo> GetBanks()
        {
            return new List<BankInfo>
            {
                new BankInfo { Name = "MBBank", AccountNumber = "0867714307", AccountHolder = "Phạm Quang Hào" },
                new BankInfo { Name = "Vietcombank", AccountNumber = "1018817774", AccountHolder = "Phạm Quang Hào" },
                new BankInfo { Name = "VPBank", AccountNumber = "0867714307", AccountHolder = "Phạm Quang Hào" }
            };
        }

        // Tạo mã QR dựa trên ngân hàng được chọn
        private string GenerateQRCode(decimal totalAmount, string bankName, string accountNumber, string accountHolder)
        {
            string bankInfo = $"{bankName}: {accountNumber} - {accountHolder}";
            string paymentInfo = $"Thanh toán đơn hàng: {totalAmount:N0} VNĐ\n{bankInfo}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(paymentInfo, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);

            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
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
                return RedirectToAction("Index", "Cart");
            }

            var banks = GetBanks();
            var defaultBank = banks.First(); // Mặc định là MB Bank

            var viewModel = new CheckoutViewModel
            {
                TotalAmount = cart.GetTotal(),
                SelectedBank = defaultBank.Name,
                QRCodeImage = GenerateQRCode(cart.GetTotal(), defaultBank.Name, defaultBank.AccountNumber, defaultBank.AccountHolder),
                Banks = banks
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetCart();
            if (cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var banks = GetBanks();
            var selectedBank = banks.FirstOrDefault(b => b.Name == model.SelectedBank) ?? banks.First();

            model.TotalAmount = cart.GetTotal();
            model.QRCodeImage = GenerateQRCode(cart.GetTotal(), selectedBank.Name, selectedBank.AccountNumber, selectedBank.AccountHolder);
            model.Banks = banks;

            return View(model);
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
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order
            {
                UserId = cart.UserId,
                OrderDate = DateTime.Now,
                TotalAmount = cart.GetTotal(),
                Status = "Completed",
                Items = new List<OrderItem>()
            };

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

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.Items);
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();

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
        public string QRCodeImage { get; set; }
        public string SelectedBank { get; set; }
        public List<BankInfo> Banks { get; set; }
    }

    public class BankInfo
    {
        public string Name { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
    }
}