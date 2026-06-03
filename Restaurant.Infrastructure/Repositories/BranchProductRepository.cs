using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;


public class BranchProductRepository : IBranchProductRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public BranchProductRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync(int branchId)
    {
        try
        {
            // return the dto intead of models to avoid circular reference issues
            // also include the branch and product details
            var branchProducts = await _context.BranchProducts
                .Where(bp => bp.BranchId == branchId )
                .Include(bp => bp.Branch)
                .Include(bp => bp.Product)
                .Select(bp => new Restaurant.Domain.Entities.BranchProductDTO
                {
                    BranchProductId = bp.BranchProductId,
                    BranchId = bp.BranchId,
                    ProductId = bp.ProductId,
                    Price = bp.Price,
                    IsActive = bp.IsActive,
                    Branch = new Restaurant.Domain.Entities.Branch
                    {
                         BranchId = bp.Branch.BranchId,
                        //HeadOfficeId = bp.Branch?.HeadOfficeId.HasValue ?,
                        BranchName = bp.Branch.BranchName,
                         AddressId =  bp.Branch.AddressId,
                         PhoneNumber =  bp.Branch.PhoneNumber,
                         Email =  bp.Branch.Email,
                         Latitude =  bp.Branch.Latitude,
                         Longitude =  bp.Branch.Longitude,
                         IsActive =  bp.Branch.IsActive,
                         IsDeleted =  bp.Branch.IsDeleted,
                    },
                    Product = new Restaurant.Domain.Entities.Product 
                    {
                        ProductId = bp.Product.ProductId,
                        ProductName = bp.Product.ProductName,
                        Description = bp.Product.Description,
                        Price = bp.Product.Price,
                        //CategoryId = bp.Product.CategoryProducts.FirstOrDefault().CategoryId,
                        //HeadOfficeId = bp.Product.HeadOfficeId.Value,
                        ImageUrl = bp.Product.ImageUrl,
                        IsAvailable = bp.Product.IsAvailable,
                        IsDeleted = bp.Product.IsDeleted,
                    }
                })

                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Branch products retrieved successfully.",
                Data = branchProducts
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        try
        {
            // return the dto intead of models to avoid circular reference issues
            // also include the branch and product details
            var branchProduct = await _context.BranchProducts
                .Where(bp => bp.BranchProductId == id)
                .Include(bp => bp.Branch)
                .Include(bp => bp.Product)
                .Select(bp => new Restaurant.Domain.Entities.BranchProductDTO
                {
                    BranchProductId = bp.BranchProductId,
                    BranchId = bp.BranchId,
                    ProductId = bp.ProductId,
                    Price = bp.Price,
                    IsActive = bp.IsActive,
                    Branch = new Restaurant.Domain.Entities.Branch
                    {
                        BranchId = bp.Branch.BranchId,
                        //HeadOfficeId = bp.Branch.HeadOfficeId.Value,
                        BranchName = bp.Branch.BranchName,
                        AddressId = bp.Branch.AddressId,
                        PhoneNumber = bp.Branch.PhoneNumber,
                        Email = bp.Branch.Email,
                        Latitude = bp.Branch.Latitude,
                        Longitude = bp.Branch.Longitude,
                        IsActive = bp.Branch.IsActive,
                        IsDeleted = bp.Branch.IsDeleted,
                    },
                    Product = new Restaurant.Domain.Entities.Product
                    {
                        ProductId = bp.Product.ProductId,
                        ProductName = bp.Product.ProductName,
                        Description = bp.Product.Description,
                        Price = bp.Product.Price,
                        //CategoryId = bp.Product.CategoryProducts.FirstOrDefault().CategoryId,
                        //HeadOfficeId = bp.Product.HeadOfficeId.Value,
                        ImageUrl = bp.Product.ImageUrl,
                        IsAvailable = bp.Product.IsAvailable,
                        IsDeleted = bp.Product.IsDeleted,
                    }
                }).FirstOrDefaultAsync();
                

            if (branchProduct == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Branch product not found."
                };
            }

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Branch product retrieved successfully.",
                Data = branchProduct
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.BranchProduct> branchProducts)
    {
        try
        {
            var entities = branchProducts.Select(bp => new Restaurant.Infrastructure.Persistence.Models.BranchProduct
            {
                BranchId = bp.BranchId,
                ProductId = bp.ProductId,
                Price = bp.Price,
                IsActive = bp.IsActive
            }).ToList();

            await _context.BranchProducts.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Branch products added successfully.",
                Data = entities
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.BranchProduct entity)
    {
        try
        {
            var branchProduct = new Restaurant.Infrastructure.Persistence.Models.BranchProduct
            {
                BranchId = entity.BranchId,
                ProductId = entity.ProductId,
                Price = entity.Price,
                IsActive = entity.IsActive

            };

            await _context.BranchProducts.AddAsync(branchProduct);
            await _context.SaveChangesAsync();


            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Branch product added successfully.",
                Data = entity
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }
    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.BranchProduct entity)
    {
        try
        {
            var existingEntity = await _context.BranchProducts.FindAsync(entity.BranchProductId);
            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Branch product not found."
                };
            }

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Branch product updated successfully.",
                Data = entity
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }
    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            var existingEntity = await _context.BranchProducts.FindAsync(id);
            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Branch product not found."
                };
            }

            _context.BranchProducts.Remove(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Branch product deleted successfully."
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }
}