using BulkyBookBusiness.Services.iServices;
using BulkyBookModels;
using BulkyBookModels.ViewModels;
using BulkyBookUtility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Client;


namespace BulkyBookWeb.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IShoppingCartService _shoppingCartService;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager, IShoppingCartService shoppingCartService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _shoppingCartService = shoppingCartService;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(loginVM.Email, loginVM.Password,
                    loginVM.RememberMe, false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    if (User.IsInRole(SD.RoleAdmin) || User.IsInRole(SD.RoleEmployee))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                    }

                    return RedirectToAction("Index", "Home", new { area = "Customer" });
                }

                ModelState.AddModelError(string.Empty, "Invalid Login Credential!");
            }

            return View(loginVM);
        }


        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var model = new RegisterVM();


            if (User.IsInRole(SD.RoleAdmin))
            {
                model.RoleList = [
                    new SelectListItem {Text = SD.RoleCustomer, Value = SD.RoleCustomer},
                    new SelectListItem {Text = SD.RoleAdmin, Value = SD.RoleAdmin},
                    new SelectListItem {Text = SD.RoleEmployee, Value = SD.RoleEmployee},
                    ];
            }
            else
            {
                model.RoleList = new List<SelectListItem>();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                
                var user = new ApplicationUser
                {
                    UserName = registerVM.Email,
                    Email = registerVM.Email,
                    Name = registerVM.Name,
                    PhoneNumber = registerVM.PhoneNumber,
                    StreetAddress = registerVM.StreetAddress,
                    City = registerVM.City,
                    State = registerVM.State,
                    PostalCode = registerVM.PostalCode
                };

                var result = await _userManager.CreateAsync(user, registerVM.Password);

                if (result.Succeeded)
                {
                    if (User.IsInRole(SD.RoleAdmin) && !string.IsNullOrEmpty(registerVM.Role))
                    {
                        await _userManager.AddToRoleAsync(user, registerVM.Role);
                        TempData["success"] = $"User '{user.Name}' created successfully with role: '{registerVM.Role}'";
                        return RedirectToAction("Index", "User", new { area = "Admin" });
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.RoleCustomer);

                        await _signInManager.SignInAsync(user, false);
                        HttpContext.Session.SetInt32(SD.SessionCart, 0);


                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Home", new { area = "Customer" });
                    }
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                registerVM.RoleList = new List<SelectListItem>
                {
                    new SelectListItem { Text = SD.RoleCustomer, Value = SD.RoleCustomer },
                    new SelectListItem { Text = SD.RoleAdmin, Value = SD.RoleAdmin },
                    new SelectListItem { Text = SD.RoleEmployee, Value = SD.RoleEmployee },
                };
            }
            return View(registerVM);
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.SetInt32(SD.SessionCart, 0);
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}
