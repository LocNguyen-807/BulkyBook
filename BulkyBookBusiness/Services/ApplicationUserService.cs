using BulkyBookBusiness.Services.iServices;
using BulkyBookDataAccess.Data;
using BulkyBookModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookBusiness.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly ApplicationDBContext _context;

        public ApplicationUserService(ApplicationDBContext context)
        {
            this._context = context;
        }


        public async Task<bool> ChangeRoleAsync(string userId, IList<string> currentRoles, string newRole)
        {
            var role = await _context.Roles
        .FirstOrDefaultAsync(r => r.Name == newRole);

            if (role == null)
                return false;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var oldRoleIds = await _context.Roles
                .Where(r => currentRoles.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync();

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId
                          && oldRoleIds.Contains(ur.RoleId))
                .ToListAsync();

            _context.UserRoles.RemoveRange(userRoles);
            await _context.SaveChangesAsync();

            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.ApplicationUsers.ToListAsync();
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
