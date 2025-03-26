using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole role)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem role đã tồn tại chưa
                var roleExists = await _roleManager.RoleExistsAsync(role.Name);
                if (!roleExists)
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Role đã tồn tại");
                }
            }
            return View(role);
        }

        public async Task<IActionResult> UserRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new List<UserRoleViewModel>();
            foreach (var role in allRoles)
            {
                model.Add(new UserRoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                });
            }

            ViewBag.UserId = userId;
            ViewBag.UserName = user.UserName;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageUserRoles(List<UserRoleViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            // Xóa tất cả roles hiện tại
            await _userManager.RemoveFromRolesAsync(user, userRoles);

            // Thêm vai trò mới được chọn
            var selectedRoles = model.Where(x => x.IsSelected).Select(r => r.RoleName).ToList();
            await _userManager.AddToRolesAsync(user, selectedRoles);

            return RedirectToAction("UserRoles");
        }
    }

    public class UserRoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}