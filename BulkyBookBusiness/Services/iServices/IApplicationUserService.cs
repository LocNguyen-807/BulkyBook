using BulkyBookModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services.iServices
{
    public interface IApplicationUserService
    {
        public Task<ApplicationUser?> GetUserByIdAsync(string userId);
        public Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<bool> ChangeRoleAsync(string userId, IList<string> currentRoles, string newRole);


    }
}
