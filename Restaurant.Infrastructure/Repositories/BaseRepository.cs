using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class BaseRepository : IBaseRepository
{
    private readonly AppDbContext _context;
    public BaseRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<ApiResponse> GetRoleCheckResultAsync(int userId)
    {
        var response = new ApiResponse();
        try
        {
            var result = new RoleCheckResult();

            // check if user exists first
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId && u.IsActive == true && u.IsDeleted != true);
            if (!userExists)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "User not found or inactive.";
                response.Data = result; // all false
                return response;
            }

            // load roles for the user
            var roles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.RoleId
                where ur.UserId == userId && ur.IsActive == true
                select r.RoleName
            ).ToListAsync();

            if (roles == null || !roles.Any())
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Message = "User has no roles assigned.";
                response.Data = result; // all false
                return response;
            }

            result.IsSystemAdmin = roles.Contains("SystemAdmin");
            result.IsSuperAdmin = roles.Contains("SuperAdmin");
            result.IsAdmin = roles.Contains("Admin");
            result.IsOrderTaker = roles.Contains("OrderTaker");

            response.StatusCode = (int)HttpStatusCode.OK;
            response.Message = "Permission check complete.";
            response.Data = result;
            return response;
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = ex.InnerException?.Message ?? ex.Message;
            response.Data = new RoleCheckResult(); // safe fallback
            return response;
        }
    }

    public class RoleCheckResult
    {
        public bool IsSystemAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOrderTaker { get; set; }
    }

}


