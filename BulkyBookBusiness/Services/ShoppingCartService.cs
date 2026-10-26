using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDBContext _context;
        public ShoppingCartService(ApplicationDBContext context)
        {
            this._context = context;
        }

        public async Task<ShoppingCart?> GetCartByIdAsync(int cartId)
        {
            return await _context.ShoppingCarts.Include(s=>s.Product).FirstOrDefaultAsync(s => s.Id == cartId);
        }
        

        public async Task ClearCartAsync(string userId)
        {
            var cartItems = await _context.ShoppingCarts.Include(s => s.Product).Where(s => s.ApplicationUserId == userId)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.ShoppingCarts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCartCountAsync(string userId)
        {
            return await _context.ShoppingCarts.Where(s => s.ApplicationUserId == userId).SumAsync(s=>s.Count);
        }

        public async Task<IEnumerable<ShoppingCart>> GetUserCartItemAsync(string userId)
        {
            return await _context.ShoppingCarts.Include(s => s.Product).Where(s => s.ApplicationUserId == userId)
                .ToListAsync();
        }

        public async Task<ShoppingCart> AddToCartAsync(ShoppingCart cart)
        {
            var existingItem = await _context.ShoppingCarts.Include(s => s.Product).FirstOrDefaultAsync(s => s.ApplicationUserId == cart.ApplicationUserId
            && s.ProductId==cart.ProductId);

            if (existingItem != null) {
                existingItem.Count += cart.Count;
                await _context.SaveChangesAsync();
                return existingItem;
            }
            cart.Id = 0;
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task UpdateCartAsync(ShoppingCart cart)
        {
            if(cart.Count <= 0)
            {
                _context.ShoppingCarts.Remove(cart);
            }
            else
            {
                _context.ShoppingCarts.Update(cart);
            }
            await _context.SaveChangesAsync();
        }
    }
}
