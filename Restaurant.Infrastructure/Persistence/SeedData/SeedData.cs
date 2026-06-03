using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Infrastructure.Persistence.SeedData
{
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.EntityFrameworkCore;
    using Restaurant.Infrastructure.Persistence;

    namespace Restaurant.Infrastructure.Persistence
    {
        public static class SeedData
        {
            public static async Task SeedSystemAdminAsync(AppDbContext context)
            {
                // Apply migrations
                //await context.Database.MigrateAsync();

                // 1️⃣ Ensure SystemAdmin role exists
                var sysAdminRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "SystemAdmin");

                if (sysAdminRole == null)
                {
                    sysAdminRole = new Models.Role
                    {
                        RoleName = "SystemAdmin",
                        RoleDescription = "System administrator with highest privileges",
                        IsActive = true
                    };

                    context.Roles.Add(sysAdminRole);
                    await context.SaveChangesAsync();
                }

                // 2️⃣ Ensure SystemAdmin user exists
                var sysAdminUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == "systemadmin");

                if (sysAdminUser == null)
                {
                    using var hmac = new HMACSHA512();
                    var salt = hmac.Key;
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes("SystemAdmin@123")); // default password

                    sysAdminUser = new Models.User
                    {
                        Username = "systemadmin",
                        Email = "dev.aliqasim@gmail.com",
                        PhoneNumber = "+923156665512",
                        PasswordSalt = salt,
                        PasswordHash = hash,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };

                    context.Users.Add(sysAdminUser);
                    await context.SaveChangesAsync();
                }

                // 3️⃣ Link SystemAdmin user with SystemAdmin role
                var userRoleExists = await context.UserRoles
                    .AnyAsync(ur => ur.UserId == sysAdminUser.UserId && ur.RoleId == sysAdminRole.RoleId);

                if (!userRoleExists)
                {
                    context.UserRoles.Add(new Models.UserRole
                    {
                        UserId = sysAdminUser.UserId,
                        RoleId = sysAdminRole.RoleId,
                        IsActive = true
                    });

                    await context.SaveChangesAsync();
                }
            }
        }
    }

}
