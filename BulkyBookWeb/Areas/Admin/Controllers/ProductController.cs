using BulkyBookBusiness.Services;
using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using BulkyBookModels.ViewModels;
using BulkyBookUtility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.Metadata;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IProductService productService, ICategoryService categoryService, IWebHostEnvironment webHostEnvironment)
        {
            _productService = productService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;

        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View("Index", products);
        }
        

        public async Task<IActionResult> Upsert(int? id)
        {
            IEnumerable<Category> categories = await _categoryService.GetAllCategoriesAsync();
            ProductVM productVM = new()
            {
                CategoryList = categories.Select(
                c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }
                ),
                Product = new Product()
            };

            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = await _productService.GetProductByIdAsync(id.Value);
                return View(productVM);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Upsert")]
        public async Task<IActionResult> UpsertPOST(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if(file != null)
                {
                    string wwwWebRoot = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine("images", "products");
                    string finalPath = Path.Combine(wwwWebRoot, productPath);

                    if (!Directory.Exists(finalPath)){
                        Directory.CreateDirectory(finalPath);
                    }

                    using(var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.ImageUrl = Path.Combine(@"\", productPath, fileName).Replace("\\", "/");
                }

                if (productVM.Product.Id == null || productVM.Product.Id == 0)
                {
                    await _productService.CreateProductAsync(productVM.Product);
                    TempData["success"] = "Successfully created product!";
                    return RedirectToAction("Index");
                }
                else
                {
                    await _productService.UpdateProductAsync(productVM.Product);
                    TempData["success"] = "Successfully updated product!";
                    return View(productVM);
                }

            }
            else
            {
                IEnumerable<Category> categories = await _categoryService.GetAllCategoriesAsync();
                productVM = new()
                {
                    CategoryList = categories.Select(
                    c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    }
                    ),
                };
                return View(productVM);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            if(id == null || id == 0)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }
            
            Product productToBeDeleted = await _productService.GetProductByIdAsync(id);
            if (productToBeDeleted == null)
            {

                return Json(new { success = false, message = "Error while deleting" });
            }

            if (!string.IsNullOrEmpty(productToBeDeleted.ImageUrl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            await _productService.DeleteProductAsync(id);
            RedirectToAction("Index");
            return Json(new { success = true, message = "Successfully deleted product!" });

        }

        #region API CAllS
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllProductsAsync(true);
            return Json(new { data = products });
        }
        #endregion
    }
}
