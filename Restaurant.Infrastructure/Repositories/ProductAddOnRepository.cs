using Azure;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class ProductAddOnRepository : IProductAddOnRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public ProductAddOnRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync(int HeadOfficeId)
    {
        try
        {
            var productAddOns = await _context.ProductAddOns
                .Include(pa => pa.Product)
                .Include(pa => pa.AddOn)
                .ThenInclude(a => a.AddOnCategory)
                .Where(pa => pa.HeadOfficeId == HeadOfficeId)
                .Select(pa => new Restaurant.Domain.Entities.ProductAddOnDTO
                {
                    Id = pa.Id,
                    ProductId = pa.ProductId,
                    AddOnId = pa.AddOnId,
                    ProductName = pa.Product.ProductName,
                    AddOnName = pa.AddOn.Name,
                    AddOnPrice = pa.AddOn.AddOnUnitPrice,
                    AddOnCategoryName = pa.AddOn.AddOnCategory.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-ons retrieved successfully.",
                Data = productAddOns
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
            var productAddOn = await _context.ProductAddOns
                .Include(pa => pa.Product)
                .Include(pa => pa.AddOn)
                .ThenInclude(a => a.AddOnCategory)
                .FirstOrDefaultAsync(pa => pa.Id == id);

            if (productAddOn == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product add-on not found."
                };
            }

            var productAddOnDto = new Restaurant.Domain.Entities.ProductAddOnDTO
            {
                Id = productAddOn.Id,
                ProductId = productAddOn.ProductId,
                AddOnId = productAddOn.AddOnId,
                ProductName = productAddOn.Product.ProductName,
                AddOnName = productAddOn.AddOn.Name,
                AddOnPrice = productAddOn.AddOn.AddOnUnitPrice,
                AddOnCategoryName = productAddOn.AddOn.AddOnCategory.Name
            };

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-on retrieved successfully.",
                Data = productAddOnDto
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

    public async Task<ApiResponse> GetByProductIdAsync(int productId)
    {
        try
        {
            var productAddOns = await _context.ProductAddOns
                .Where(pa => pa.ProductId == productId)
                .Include(pa => pa.Product)
                .Include(pa => pa.AddOn)
                .ThenInclude(a => a.AddOnCategory)
                .Select(pa => new Restaurant.Domain.Entities.ProductAddOnDTO
                {
                    Id = pa.Id,
                    ProductId = pa.ProductId,
                    AddOnId = pa.AddOnId,
                    ProductName = pa.Product.ProductName,
                    AddOnName = pa.AddOn.Name,
                    AddOnPrice = pa.AddOn.AddOnUnitPrice,
                    AddOnCategoryName = pa.AddOn.AddOnCategory.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-ons retrieved successfully.",
                Data = productAddOns
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

    public async Task<ApiResponse> GetByAddOnIdAsync(int addOnId)
    {
        try
        {
            var productAddOns = await _context.ProductAddOns
                .Where(pa => pa.AddOnId == addOnId)
                .Include(pa => pa.Product)
                .Include(pa => pa.AddOn)
                .ThenInclude(a => a.AddOnCategory)
                .Select(pa => new Restaurant.Domain.Entities.ProductAddOnDTO
                {
                    Id = pa.Id,
                    ProductId = pa.ProductId,
                    AddOnId = pa.AddOnId,
                    ProductName = pa.Product.ProductName,
                    AddOnName = pa.AddOn.Name,
                    AddOnPrice = pa.AddOn.AddOnUnitPrice,
                    AddOnCategoryName = pa.AddOn.AddOnCategory.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-ons retrieved successfully.",
                Data = productAddOns
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.ProductAddOn> productAddOns)
    {
        var response = new ApiResponse();
        try
        {
            if (productAddOns == null || productAddOns.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Product Addons provided.";
                return response;
            }

            // Validate HeadOfficeIds
            var headOfficeIds = productAddOns.Select(a => a.HeadOfficeId).Distinct().ToList();
            var validHeadOfficeIds = await _context.HeadOffices
                .AsNoTracking()
                .Where(c => headOfficeIds.Contains(c.HeadOfficeId) &&
                            (c.IsActive == true))
                .Select(c => c.HeadOfficeId)
                .ToListAsync();
            var invalidHeadOfficeIds = headOfficeIds.Except(validHeadOfficeIds).ToList();
            if (invalidHeadOfficeIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOffice(s): {string.Join(", ", invalidHeadOfficeIds)}. Please provide valid and active HeadOffices.";
                return response;
            }

            // Validate ProductIds
            var productIds = productAddOns.Select(a => a.ProductId).Distinct().ToList();
            var validProductIds = await _context.Products
                .AsNoTracking()
                .Where(c => productIds.Contains(c.ProductId) && c.IsDeleted != true)
                .Select(c => c.ProductId)
                .ToListAsync();
            var invalidProductIds = productIds.Except(validProductIds).ToList();
            if (invalidProductIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid ProductIds(s): {string.Join(", ", invalidProductIds)}. Please provide valid Products.";
                return response;
            }

            // Validate AddonIds
            var addonIds = productAddOns.Select(a => a.AddOnId).Distinct().ToList();
            var validAddonIds = await _context.AddOns
                .AsNoTracking()
                .Where(c => addonIds.Contains(c.Id) && c.IsDeleted != true)
                .Select(c => c.Id)
                .ToListAsync();
            var invalidAddonIds = addonIds.Except(validAddonIds).ToList();
            if (invalidAddonIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid AddonId(s): {string.Join(", ", invalidAddonIds)}. Please provide valid Addons.";
                return response;
            }

            foreach (var productAddon in productAddOns)
            {
                bool productAddonDuplicate = await _context.ProductAddOns.AnyAsync(x => x.ProductId == productAddon.ProductId && x.AddOnId == productAddon.AddOnId && x.HeadOfficeId == productAddon.HeadOfficeId);
                if (productAddonDuplicate) // if duplicate exists
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = $"Product Addon Already exists. Details: [ ProductId:{productAddon.ProductId}, AddonId:{productAddon.AddOnId} ]";
                    response.Data = null;
                    return response;
                }
            }

            var productAddOnList = productAddOns.Select(entity => new Restaurant.Infrastructure.Persistence.Models.ProductAddOn
            {
                ProductId = entity.ProductId,
                AddOnId = entity.AddOnId,
                HeadOfficeId = entity.HeadOfficeId,
            }).ToList();

            await _context.ProductAddOns.AddRangeAsync(productAddOnList);
            await _context.SaveChangesAsync();


            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Product add-on added successfully.",
                Data = productAddOnList
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


    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.ProductAddOn entity)
    {
        var response = new ApiResponse();
        try
        {
            var isValidHeadOffice = await _context.HeadOffices.AnyAsync(x => x.HeadOfficeId == entity.HeadOfficeId && x.IsActive == true);
            if (!isValidHeadOffice)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid HeadOffice.";
                response.Data = null;
                return response;
            }

            var isProductValid = await _context.Products.AnyAsync(x => x.ProductId == entity.ProductId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isProductValid)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Product.";
                response.Data = null;
                return response;
            }

            var isValidAddonId = await _context.AddOns.AnyAsync(x => x.Id == entity.AddOnId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isValidAddonId)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Addon.";
                response.Data = null;
                return response;
            }

            // Check if the relationship already exists
            var existing = await _context.ProductAddOns
                .FirstOrDefaultAsync(pa => pa.ProductId == entity.ProductId && pa.AddOnId == entity.AddOnId && pa.HeadOfficeId == entity.HeadOfficeId);

            if (existing != null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "This add-on is already associated with the product."
                };
            }

            var productAddOn = new Restaurant.Infrastructure.Persistence.Models.ProductAddOn
            {
                ProductId = entity.ProductId,
                AddOnId = entity.AddOnId,
                HeadOfficeId = entity.HeadOfficeId,
            };

            await _context.ProductAddOns.AddAsync(productAddOn);
            await _context.SaveChangesAsync();

            // Map back to domain entity with generated ID
            entity.Id = productAddOn.Id;

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Product add-on added successfully.",
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

    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.ProductAddOn entity)
    {
        var response = new ApiResponse();
        try
        {
            var existingEntity = await _context.ProductAddOns
                .FirstOrDefaultAsync(pa => pa.Id == entity.Id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product add-on not found."
                };
            }

            var isValidHeadOffice = await _context.HeadOffices.AnyAsync(x => x.HeadOfficeId == entity.HeadOfficeId && x.IsActive == true);
            if (!isValidHeadOffice)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid HeadOffice.";
                response.Data = null;
                return response;
            }

            var isProductValid = await _context.Products.AnyAsync(x => x.ProductId == entity.ProductId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isProductValid)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Product.";
                response.Data = null;
                return response;
            }

            var isValidAddonId = await _context.AddOns.AnyAsync(x => x.Id == entity.AddOnId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isValidAddonId)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Addon.";
                response.Data = null;
                return response;
            }


            // Check if the new relationship already exists (for different ID)
            var duplicate = await _context.ProductAddOns
                .FirstOrDefaultAsync(pa => pa.ProductId == entity.ProductId &&
                                         pa.AddOnId == entity.AddOnId &&
                                         pa.HeadOfficeId == entity.HeadOfficeId &&
                                         pa.Id != entity.Id);

            if (duplicate != null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "This add-on is already associated with the product."
                };
            }

            // Update properties
            existingEntity.ProductId = entity.ProductId;
            existingEntity.AddOnId = entity.AddOnId;
            existingEntity.HeadOfficeId = entity.HeadOfficeId;

            _context.ProductAddOns.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-on updated successfully.",
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
            var existingEntity = await _context.ProductAddOns
                .FirstOrDefaultAsync(pa => pa.Id == id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product add-on not found."
                };
            }

            _context.ProductAddOns.Remove(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product add-on deleted successfully."
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