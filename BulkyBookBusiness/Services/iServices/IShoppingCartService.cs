using BulkyBookModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services.iServices
{
    public interface IShoppingCartService
    {
        Task<IEnumerable<ShoppingCart>> GetUserCartItemAsync(string userId);
        Task<ShoppingCart?> GetCartByIdAsync(int cartId);
        Task<int> GetCartCountAsync(string userId);
        Task<ShoppingCart> AddToCartAsync(ShoppingCart cart);
        Task UpdateCartAsync(ShoppingCart cart);
        Task ClearCartAsync(string userId);


    }
}
