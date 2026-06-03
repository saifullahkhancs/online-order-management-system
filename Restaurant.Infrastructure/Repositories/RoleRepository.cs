using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles.Where(x => x.RoleName != "SystemAdmin")
                .Select(r => new Role
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    RoleDescription = r.RoleDescription,
                    IsActive = r.IsActive ?? true
                })
                .ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return null;

            return new Role
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                RoleDescription = role.RoleDescription,
                IsActive = role.IsActive ?? true
            };
        }

        public async Task<Role> AddAsync(Role role)
        {
            var dbRole = new Persistence.Models.Role
            {
                RoleName = role.RoleName,
                RoleDescription = role.RoleDescription,
                IsActive = role.IsActive
            };

            _context.Roles.Add(dbRole);
            await _context.SaveChangesAsync();

            role.RoleId = dbRole.RoleId;
            return role;
        }

        public async Task<Role> UpdateAsync(Role role)
        {
            var dbRole = await _context.Roles.FindAsync(role.RoleId);
            if (dbRole == null) throw new Exception("Role not found");

            dbRole.RoleName = role.RoleName;
            dbRole.RoleDescription = role.RoleDescription;
            dbRole.IsActive = role.IsActive;

            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            role.IsActive = false;
            _context.Update(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
