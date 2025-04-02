using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Hiển thị danh sách người dùng
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Fullname = user.Fullname,
                    Address = user.Address,
                    Age = user.Age,
                    Role = roles.FirstOrDefault()
                });
            }

            return View(userViewModels);
        }

        // Xem chi tiết người dùng (gộp cả thông tin cơ bản và mở rộng)
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Cart) // Lấy thông tin giỏ hàng
                .ThenInclude(c => c.Items) // Lấy các mục trong giỏ hàng
                .Include(u => u.Orders) // Lấy danh sách đơn hàng
                .ThenInclude(o => o.Items) // Lấy các mục trong đơn hàng
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Fullname = user.Fullname,
                Address = user.Address,
                Age = user.Age,
                Role = roles.FirstOrDefault(),
                Cart = user.Cart,
                Orders = user.Orders
            };
            return View(model);
        }

        // Thêm người dùng (GET)
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View();
        }

        // Thêm người dùng (POST)
        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel model, string role)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    Fullname = model.Fullname,
                    Address = model.Address,
                    Age = model.Age
                };
                var result = await _userManager.CreateAsync(user, "DefaultPassword123!"); // Mật khẩu mặc định
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(model);
        }

        // Sửa người dùng (GET)
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Fullname = user.Fullname,
                Address = user.Address,
                Age = user.Age,
                Role = roles.FirstOrDefault()
            };
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(model);
        }

        // Sửa người dùng (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(UserViewModel model, string role)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null) return NotFound();

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.Fullname = model.Fullname;
                user.Address = model.Address;
                user.Age = model.Age;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(model);
        }

        // Xóa người dùng (GET)
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Fullname = user.Fullname,
                Address = user.Address,
                Age = user.Age,
                Role = roles.FirstOrDefault()
            };
            return View(model);
        }

        // Xóa người dùng (POST)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction("Index");
        }
    }
}