using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDBContext _context;

        public ProductService(ApplicationDBContext context)
        {
            this._context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategory = false)
        {
            if (includeCategory)
            {
                return await _context.Products.Include(p => p.Category).ToListAsync();
            }
            return await _context.Products.ToListAsync();
        }


        public async Task<Product?> GetProductByIdAsync(int id, bool includeCategory = false)
        {
            if (includeCategory)
            {
                return await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            }
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task DeleteProductAsync(int id)
        {
            Product product = _context.Products.Find(id);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product {id} not found");
            }

            _context.Products.Remove(product);
            _context.SaveChanges();
        }



        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        
    }
}
