using BulkyBookBusiness.Services.iServices;
using BulkyBookModels;
using BulkyBookUtility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;

        public HomeController(IProductService productService, IShoppingCartService shoppingCartService)
        {
            _productService = productService;
            _shoppingCartService = shoppingCartService;
        }


        public async Task<IActionResult> Index()
        {
            IEnumerable<Product> products = await _productService.GetAllProductsAsync(true);
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            Product product = await _productService.GetProductByIdAsync(id, true);

            if(product == null)
            {
                return NotFound();
            }

            ShoppingCart cart = new ()
            {
                Product = product,
                Count = 1,
                ProductId = id
            };
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Details([Bind("ProductId,Count")] ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;

            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            shoppingCart.ApplicationUserId = userId;

            await _shoppingCartService.AddToCartAsync(shoppingCart);

            var count = await _shoppingCartService.GetCartCountAsync(userId);
            HttpContext.Session.SetInt32(SD.SessionCart, count);

            TempData["success"] = "Product added to cart";

            return RedirectToAction("Index");
        }


        
    }
}
