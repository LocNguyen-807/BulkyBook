using BulkyBookBusiness.Services;
using BulkyBookBusiness.Services.iServices;
using BulkyBookModels;
using BulkyBookModels.ViewModels;
using BulkyBookUtility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]
    public class UserController : Controller
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IApplicationUserService applicationUserService, UserManager<ApplicationUser> userManager)
        {
            _applicationUserService = applicationUserService;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ChangePassword(string userId)
        {
            var user = await _applicationUserService.GetUserByIdAsync(userId);

            if(user == null)
            {
                return NotFound("User not found.");
            }

            AdminChangePasswordVM adminChangePasswordVM = new()
            {
                UserId = userId,
                UserEmail = user.Email,
                UserName = user.Name,
            };

            return View(adminChangePasswordVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ChangePassword")]
        public async Task<IActionResult> ChangePasswordPOST(AdminChangePasswordVM adminChangePasswordVM)
        {
            var user = await _applicationUserService.GetUserByIdAsync(adminChangePasswordVM.UserId);

            if (user == null)
                return NotFound("User not found.");

            if (ModelState.IsValid)
            {

            if (string.IsNullOrWhiteSpace(adminChangePasswordVM.NewPassword)
                || string.IsNullOrWhiteSpace(adminChangePasswordVM.ConfirmPassword)
                || adminChangePasswordVM.NewPassword != adminChangePasswordVM.ConfirmPassword)
            {
                TempData["error"] = "Passwords are empty or do not match!";

                return RedirectToAction(
                    nameof(ChangePassword), new { userId = user.Id });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user, token, adminChangePasswordVM.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = "Reset password successfully.";
            }
            else
            {
                TempData["error"] = string.Join(
                    "; ", result.Errors.Select(e => e.Description));
            }

            }
            return RedirectToAction(
                nameof(ChangePassword), new { userId = user.Id });
        }

        public async Task<IActionResult> Role(string userId)
        {
            ApplicationUser? user = await _applicationUserService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            RoleManagerVM roleManagerVM = new RoleManagerVM
            {
                ApplicationUser = user,
                RoleList = new List<SelectListItem>
                {
                    new SelectListItem { Text = SD.RoleCustomer, Value=SD.RoleCustomer},
                    new SelectListItem { Text = SD.RoleAdmin, Value=SD.RoleAdmin},
                    new SelectListItem { Text = SD.RoleEmployee, Value=SD.RoleEmployee},
                },
                CurrentRole = string.Join(", ", roles),
            };

            return View(roleManagerVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Role")]
        public async Task<IActionResult> RolePOST(RoleManagerVM roleManagerVM)
        {
            var userId = roleManagerVM.ApplicationUser?.Id;

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Missing user ID.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            var success = await _applicationUserService.ChangeRoleAsync(
                user.Id,
                currentRoles,
                roleManagerVM.SelectedRole);

            TempData[success ? "success" : "error"] = success
                ? "Role updated successfully!"
                : "Invalid role.";

            return RedirectToAction("Role", new { userId = user.Id });
        }

        [HttpPost]
        [ActionName("UserLock")]
        public async Task<IActionResult> UserLock(string userId)
        {
            var user = await _applicationUserService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (await _userManager.IsInRoleAsync(user, SD.RoleAdmin))
            {
                TempData["error"] = "Cannot lock/unlock an admin account.";
                return RedirectToAction("Index");
            }

            if (!await _userManager.GetLockoutEnabledAsync(user))
            {
                TempData["error"] = "Lockout is not enabled for this user.";
                return RedirectToAction("Index");
            }

            bool isLocked = await _userManager.IsLockedOutAsync(user);

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                isLocked ? (DateTimeOffset?)null : DateTimeOffset.MaxValue
            );

            if (result.Succeeded)
            {
                TempData["success"] = isLocked
                    ? "User is unlocked successfully!"
                    : "User is locked successfully!";
            }
            else
            {
                TempData["error"] = string.Join(
                    "; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }


        #region API CAllS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _applicationUserService.GetAllUsersAsync();

            var data = new List<object>();

            foreach(var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);

                data.Add(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    role = string.Join(", ", roles),
                    isLocked = isLocked
                });
            }

            return Json(new { data });
        }
        #endregion
    }
}
