using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public ProductRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    //public async Task<ApiResponse> GetAllAsync()
    //{
    //    try
    //    {
    //        var products = await _context.Products
    //            .Where(p => !p.IsDeleted)
    //            .ToListAsync();

    //        return new ApiResponse
    //        {
    //            StatusCode = (int)HttpStatusCode.OK,
    //            Message = "Products retrieved successfully.",
    //            Data = products
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ApiResponse
    //        {
    //            StatusCode = (int)HttpStatusCode.InternalServerError,
    //            Message = $"An error occurred: {ex.Message}"
    //        };
    //    }
    //}

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
                apiResponse.Message = "Unauthorized user.";
                apiResponse.Data = new List<ProductDTO>();
                return apiResponse;
            }

            // Step 2: Start base query (not filtered yet)
            var query = _context.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.CategoryProducts)
                    .ThenInclude(cp => cp.Category)
                .AsQueryable();

            // Step 3: Apply role-based filtering
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin  can view all products
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin  products of their own HeadOffice
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(p => p.HeadOfficeId == headOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin / OrderTaker  products under their branch’s HeadOffice
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == false || ub.IsDeleted == null)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                query = query.Where(p => p.HeadOfficeId == branchHeadOfficeId);
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                apiResponse.Message = "User does not have permission to view products.";
                apiResponse.Data = new List<ProductDTO>();
                return apiResponse;
            }

            // Step 4: Projection to DTO
            var products = await query
                .AsNoTracking()
                .Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    IsAvailable = p.IsAvailable,
                    IsDeleted = p.IsDeleted,
                    HeadOfficeId = p.HeadOfficeId.HasValue ? p.HeadOfficeId.Value : 0,
                    Categories = p.CategoryProducts
                        .Where(cp => cp.IsActive)
                        .Select(cp => new CategoryDTO
                        {
                            CategoryId = cp.Category.CategoryId,
                            CategoryName = cp.Category.CategoryName,
                            Description = cp.Category.Description
                        }).ToList()
                })
                .ToListAsync();

            // Step 5: Return response
            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Products retrieved successfully.";
            apiResponse.Data = products;
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
            var product = _context.Products
                .Where(p => !p.IsDeleted && p.ProductId == id)
                .Include(p => p.CategoryProducts)
                    .ThenInclude(cp => cp.Category)
                .AsNoTracking()
                .Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    IsAvailable = p.IsAvailable,
                    IsDeleted = p.IsDeleted,
                    HeadOfficeId = p.HeadOfficeId.HasValue ? p.HeadOfficeId.Value : 0,
                    Categories = p.CategoryProducts
                        .Where(cp => cp.IsActive)
                        .Select(cp => new CategoryDTO
                        {
                            CategoryId = cp.Category.CategoryId,
                            CategoryName = cp.Category.CategoryName,
                            Description = cp.Category.Description
                        }).ToList()
                })
                .FirstOrDefault();

            if (product == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product not found."
                };
            }

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product retrieved successfully.",
                Data = product
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

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.Product entity)
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
                apiResponse.Data = new List<Restaurant.Domain.Entities.Product>();
                return apiResponse;
            }

            // Step 2: Apply role-based HeadOffice mapping
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin  use provided HeadOfficeId
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin  assign HeadOfficeId from logged-in user
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                if (headOfficeId.HasValue)
                    entity.HeadOfficeId = headOfficeId.Value;
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin / OrderTaker  assign HeadOfficeId from their branch
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == false || ub.IsDeleted == null)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                if (branchHeadOfficeId.HasValue)
                    entity.HeadOfficeId = branchHeadOfficeId.Value;
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "User does not have permission to create products.";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Product>();
                return apiResponse;
            }

            // Step 3: Validate HeadOfficeId
            if (entity.HeadOfficeId == 0)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "HeadOfficeId could not be resolved for this user.";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Step 4: Prevent duplicate products under same HeadOffice
            var isAlreadyExist = await _context.Products
                .AnyAsync(x => x.ProductName.Trim().ToLower() == entity.ProductName.Trim().ToLower()
                            && x.HeadOfficeId == entity.HeadOfficeId
                            && (x.IsDeleted != true));

            if (isAlreadyExist)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Product already exists against the same HeadOffice.";
                apiResponse.Data = new List<Restaurant.Domain.Entities.Product>();
                return apiResponse;
            }

            // Step 5: Create and save product
            var product = new Restaurant.Infrastructure.Persistence.Models.Product
            {
                ProductName = entity.ProductName.Trim(),
                Description = entity.Description,
                Price = entity.Price,
                ImageUrl = entity.ImageUrl,
                IsAvailable = entity.IsAvailable,
                IsDeleted = false,
                HeadOfficeId = entity.HeadOfficeId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Step 6: Link product to category
            var categoryProduct = new CategoryProduct
            {
                ProductId = product.ProductId,
                CategoryId = entity.CategoryId,
                IsActive = true
            };

            _context.CategoryProducts.Add(categoryProduct);
            await _context.SaveChangesAsync();

            // Step 7: Return success
            apiResponse.StatusCode = (int)HttpStatusCode.Created;
            apiResponse.Message = "Product added successfully.";
            apiResponse.Data = "Product added successfully.";
            return apiResponse;
        }
        catch (Exception ex)
        {
            apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            apiResponse.Message = $"An error occurred: {ex.Message}";
            return apiResponse;
        }
    }


    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.Product entity)
    {
        var apiResponse = new ApiResponse();
        try
        {
            // 1) Ensure logged-in user
            var userIdNullable = _userContext.GetUserId();
            if (!userIdNullable.HasValue)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized user.";
                return apiResponse;
            }
            var userId = userIdNullable.Value;

            // 2) Role check
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
            var roleCheck = roleCheckResponse?.Data as BaseRepository.RoleCheckResult;
            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized user.";
                return apiResponse;
            }

            // 3) Load product (and its category mappings)
            var existingProduct = await _context.Products
                .Include(p => p.CategoryProducts)
                .FirstOrDefaultAsync(p => p.ProductId == entity.ProductId && (p.IsDeleted == false || p.IsDeleted == null));
            if (existingProduct == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Product not found.";
                return apiResponse;
            }

            // 4) Apply head-office mapping rules (same approach as AddAsync)
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin: keep incoming entity.HeadOfficeId (no mapping)
            }
            else if (roleCheck.IsSuperAdmin)
            {
                var headOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                if (!headOfficeId.HasValue)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "SuperAdmin does not belong to any HeadOffice.";
                    return apiResponse;
                }

                entity.HeadOfficeId = headOfficeId.Value;
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                var branchHeadOfficeId = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                    select b.HeadOfficeId
                ).FirstOrDefaultAsync();

                if (!branchHeadOfficeId.HasValue)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "User is not assigned to any active branch.";
                    return apiResponse;
                }

                entity.HeadOfficeId = branchHeadOfficeId.Value;
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                apiResponse.Message = "User does not have permission to update products.";
                return apiResponse;
            }

            //// 5) Authorization enforcement: non-system admins can only update products in their headoffice
            //if (!roleCheck.IsSystemAdmin && existingProduct.HeadOfficeId != entity.HeadOfficeId)
            //{
            //    apiResponse.StatusCode = (int)HttpStatusCode.Forbidden;
            //    apiResponse.Message = "You are not authorized to update this product.";
            //    return apiResponse;
            //}

            // 6) Update product fields
            existingProduct.ProductName = entity.ProductName;
            existingProduct.Description = entity.Description;
            existingProduct.Price = entity.Price;
            existingProduct.ImageUrl = entity.ImageUrl;
            existingProduct.IsAvailable = entity.IsAvailable;
            existingProduct.HeadOfficeId = entity.HeadOfficeId;

            // 7) Replace category mappings (keep existing pattern)
            if (existingProduct.CategoryProducts != null && existingProduct.CategoryProducts.Any())
            {
                _context.CategoryProducts.RemoveRange(existingProduct.CategoryProducts);
            }

            // Only add mapping if a category was provided (mirrors existing Add/Update patterns)
            if (entity.CategoryId > 0)
            {
                var categoryProduct = new CategoryProduct
                {
                    ProductId = existingProduct.ProductId,
                    CategoryId = entity.CategoryId,
                    IsActive = true
                };
                _context.CategoryProducts.Add(categoryProduct);
            }

            await _context.SaveChangesAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Product updated successfully.";
            apiResponse.Data = "Product updated successfully.";
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


    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null || existingProduct.IsDeleted)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product not found."
                };
            }

            existingProduct.IsDeleted = true;
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product deleted successfully."
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