using BulkyBookModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services.iServices
{
    public interface IProductService
    {
        Task<Product?> GetProductByIdAsync(int id, bool includeCategory = false);
        Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategory = false);
        Task<Product> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);

        //Task<bool> IsProductNameUniqueAsync(string name, int? productId = null);
    }
}
