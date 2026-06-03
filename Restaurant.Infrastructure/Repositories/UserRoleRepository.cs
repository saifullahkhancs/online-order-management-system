using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserWithRolesDto> AssignUserRoles(int userId, IEnumerable<int> newRoleIds)
        {
            // Get existing roles
            var existingRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            var existingRoleIds = existingRoles.Select(r => r.RoleId).ToList();

            // Roles to remove
            var rolesToRemove = existingRoles
                .Where(r => !newRoleIds.Contains(r.RoleId))
                .ToList();

            // Roles to add
            var rolesToAdd = newRoleIds
                .Where(roleId => !existingRoleIds.Contains(roleId))
                .Select(roleId => new Persistence.Models.UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    IsActive = true
                }).ToList();

            // Apply changes
            if (rolesToRemove.Any())
                _context.UserRoles.RemoveRange(rolesToRemove);

            if (rolesToAdd.Any())
                await _context.UserRoles.AddRangeAsync(rolesToAdd);

            await _context.SaveChangesAsync();

            // Return updated role list
            return await GetUserWithRolesAsync(userId);
        }
        

        public async Task<UserWithRolesDto?> GetUserWithRolesAsync(int userId)
        {
            var dbUser = await _context.Users
                .Where(u => u.UserId == userId && u.IsDeleted == false)
                .Select(u => new UserWithRolesDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Roles = (
                        from ur in _context.UserRoles
                        join r in _context.Roles on ur.RoleId equals r.RoleId
                        where ur.UserId == u.UserId && ur.IsActive == true
                        select new RoleTupleDto(r.RoleId, r.RoleName!)
                    ).ToList()
                })
                .FirstOrDefaultAsync();

            return dbUser;
        }


    }
}
