using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public CategoryRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync()
    {
        var apiResponse = new ApiResponse();

        try
        {
            var userId = _userContext.GetUserId().Value;

            // Step 1: Check user role
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId!);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }

            // Step 2: Start base query
            var query = _context.Categories
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            // Step 3: Apply role-based filtering
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin  can see all categories
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin  only categories under their HeadOffice
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(c => c.HeadOfficeId == headOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin or OrderTaker  only categories belonging to their branch’s HeadOffice
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == false || ub.IsDeleted == null)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                query = query.Where(c => c.HeadOfficeId == branchHeadOfficeId);
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "User does not have permission to view categories.";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }

            // Step 4: Execute final query
            var categories = await query
                .AsNoTracking()
                .ToListAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Categories retrieved successfully.";
            apiResponse.Data = categories;
            return apiResponse;
        }
        catch (Exception ex)
        {
            apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
            apiResponse.Data = null;
            return apiResponse;
        }
    }


    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && !c.IsDeleted);

            if (category == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Category not found."
                };
            }

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Category retrieved successfully.",
                Data = category
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.Category entity)
    {
        var apiResponse = new ApiResponse();
        try
        {
            
            var userId = _userContext.GetUserId().Value;

            // Step 1: Check user role
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();

            }

            // Step 3: Apply role-based filtering
            if (roleCheck.IsSystemAdmin)
            {
                // Do nothing, assume payload is OK
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin  only categories under their HeadOffice
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                // Assign User HeadOffice Id to the payload HeadOffice Id
                if (headOfficeId.HasValue == true)
                {
                    entity.HeadOfficeId = headOfficeId.Value;
                }
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin or OrderTaker  only categories belonging to their branch’s HeadOffice
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == false || ub.IsDeleted == null)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                // Assign User HeadOffice Id to the payload HeadOffice Id
                if (branchHeadOfficeId.HasValue == true)
                {
                    entity.HeadOfficeId = branchHeadOfficeId.Value;
                }
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "User does not have permission to create categories.";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }


            var isAlreadyExist = _context.Categories
                                .Any(x => x.CategoryName.Trim().ToLower() == entity.CategoryName.Trim().ToLower()
                                       && x.HeadOfficeId == entity.HeadOfficeId
                                       && (x.IsDeleted != true));
            if(isAlreadyExist == true)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Category already exist against the HeadOffice";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }

            var category = new Restaurant.Infrastructure.Persistence.Models.Category
            {
                CategoryName = entity.CategoryName.Trim(),
                Description = entity.Description,
                ImageUrl = entity.ImageUrl,
                IsActive = entity.IsActive,
                IsDeleted = false,
                HeadOfficeId = entity.HeadOfficeId
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Category added successfully.",
                Data = category
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"An error occurred: {ex.Message}"
            };

        }
    }

    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.Category entity)
    {
        var apiResponse = new ApiResponse();
        try
        {
            var userId = _userContext.GetUserId().Value;

            // Step 1: Check user role
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();

            }

            // Step 3: Apply role-based filtering
            if (roleCheck.IsSystemAdmin)
            {
                // Do nothing, assume payload is OK
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin  only categories under their HeadOffice
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                // Assign User HeadOffice Id to the payload HeadOffice Id
                if (headOfficeId.HasValue == true)
                {
                    entity.HeadOfficeId = headOfficeId.Value;
                }
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin or OrderTaker  only categories belonging to their branch’s HeadOffice
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == false || ub.IsDeleted == null)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                // Assign User HeadOffice Id to the payload HeadOffice Id
                if (branchHeadOfficeId.HasValue == true)
                {
                    entity.HeadOfficeId = branchHeadOfficeId.Value;
                }
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "User does not have permission to update categories.";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }


            var isAlreadyExist = _context.Categories
                                .Any(x => x.CategoryName.Trim().ToLower() == entity.CategoryName.Trim().ToLower()
                                       && x.CategoryId != entity.CategoryId && x.HeadOfficeId == entity.HeadOfficeId
                                       && (x.IsDeleted != true));
            if (isAlreadyExist == true)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Category already exist against the HeadOffice";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Category>();
                return apiResponse;
            }

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == entity.CategoryId && c.HeadOfficeId == entity.HeadOfficeId && !c.IsDeleted);

            if (existingCategory == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Category not found."
                };
            }

            existingCategory.CategoryName = entity.CategoryName;
            existingCategory.Description = entity.Description;
            existingCategory.IsActive = entity.IsActive;
            existingCategory.ImageUrl = entity.ImageUrl;
            existingCategory.HeadOfficeId = entity.HeadOfficeId;    
            // Note: IsDeleted should not be updated here
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Category updated successfully.",
                Data = existingCategory
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && !c.IsDeleted);

            if (existingCategory == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Category not found."
                };
            }

            // Soft delete
            existingCategory.IsDeleted = true;
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Category deleted successfully."
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }
}
