using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class ProductModifierRepository : IProductModifierRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public ProductModifierRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync(int HeadOfficeId)
    {
        try
        {
            var productModifiers = await _context.ProductModifiers
                .Include(pm => pm.Product)
                .Include(pm => pm.Modifier)
                .ThenInclude(m => m.Category)
                .Where(pm => pm.HeadOfficeId == HeadOfficeId)
                .Select(pm => new Restaurant.Domain.Entities.ProductModifierDTO
                {
                    Id = pm.Id,
                    ProductId = pm.ProductId,
                    ModifierId = pm.ModifierId,
                    ProductName = pm.Product.ProductName,
                    ModifierName = pm.Modifier.Name,
                    ModifierPrice = pm.Modifier.DefaultPrice,
                    ModifierCategoryName = pm.Modifier.Category.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifiers retrieved successfully.",
                Data = productModifiers
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
            var productModifier = await _context.ProductModifiers
                .Include(pm => pm.Product)
                .Include(pm => pm.Modifier)
                .ThenInclude(m => m.Category)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (productModifier == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product modifier not found."
                };
            }

            var productModifierDto = new Restaurant.Domain.Entities.ProductModifierDTO
            {
                Id = productModifier.Id,
                ProductId = productModifier.ProductId,
                ModifierId = productModifier.ModifierId,
                ProductName = productModifier.Product.ProductName,
                ModifierName = productModifier.Modifier.Name,
                ModifierPrice = productModifier.Modifier.DefaultPrice,
                ModifierCategoryName = productModifier.Modifier.Category.Name
            };

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifier retrieved successfully.",
                Data = productModifierDto
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
            var productModifiers = await _context.ProductModifiers
                .Where(pm => pm.ProductId == productId)
                .Include(pm => pm.Product)
                .Include(pm => pm.Modifier)
                .ThenInclude(m => m.Category)
                .Select(pm => new Restaurant.Domain.Entities.ProductModifierDTO
                {
                    Id = pm.Id,
                    ProductId = pm.ProductId,
                    ModifierId = pm.ModifierId,
                    ProductName = pm.Product.ProductName,
                    ModifierName = pm.Modifier.Name,
                    ModifierPrice = pm.Modifier.DefaultPrice,
                    ModifierCategoryName = pm.Modifier.Category.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifiers retrieved successfully.",
                Data = productModifiers
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

    public async Task<ApiResponse> GetByModifierIdAsync(int modifierId)
    {
        try
        {
            var productModifiers = await _context.ProductModifiers
                .Where(pm => pm.ModifierId == modifierId)
                .Include(pm => pm.Product)
                .Include(pm => pm.Modifier)
                .ThenInclude(m => m.Category)
                .Select(pm => new Restaurant.Domain.Entities.ProductModifierDTO
                {
                    Id = pm.Id,
                    ProductId = pm.ProductId,
                    ModifierId = pm.ModifierId,
                    ProductName = pm.Product.ProductName,
                    ModifierName = pm.Modifier.Name,
                    ModifierPrice = pm.Modifier.DefaultPrice,
                    ModifierCategoryName = pm.Modifier.Category.Name
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifiers retrieved successfully.",
                Data = productModifiers
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.ProductModifier> productModifiers)
    {
        var response = new ApiResponse();
        try
        {
            if (productModifiers == null || productModifiers.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Product Modifier provided.";
                return response;
            }

            // Validate HeadOfficeIds
            var headOfficeIds = productModifiers.Select(a => a.HeadOfficeId).Distinct().ToList();
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
            var productIds = productModifiers.Select(a => a.ProductId).Distinct().ToList();
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

            // Validate modifierIds
            var modifierIds = productModifiers.Select(a => a.ModifierId).Distinct().ToList();
            var validModifierIds = await _context.Modifiers
                .AsNoTracking()
                .Where(c => modifierIds.Contains(c.Id) && c.IsDeleted != true)
                .Select(c => c.Id)
                .ToListAsync();
            var invalidModifierIds = modifierIds.Except(validModifierIds).ToList();
            if (invalidModifierIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid ModifierId(s): {string.Join(", ", invalidModifierIds)}. Please provide valid Modifiers.";
                return response;
            }

            foreach (var productModifier in productModifiers)
            {
                bool productModifierDuplicate = await _context.ProductModifiers.AnyAsync(x => x.ProductId == productModifier.ProductId && x.ModifierId == productModifier.ModifierId && x.HeadOfficeId == productModifier.HeadOfficeId);
                if (productModifierDuplicate) // if duplicate exists
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = $"Product Addon Already exists. Details: [ ProductId:{productModifier.ProductId}, AddonId:{productModifier.ModifierId} ]";
                    response.Data = null;
                    return response;
                }
            }

            var productModifierList = productModifiers.Select(entity => new Restaurant.Infrastructure.Persistence.Models.ProductModifier
            {
                ProductId = entity.ProductId,
                ModifierId = entity.ModifierId,
                HeadOfficeId = entity.HeadOfficeId,
            }).ToList();

            await _context.ProductModifiers.AddRangeAsync(productModifierList);
            await _context.SaveChangesAsync();


            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Product modifier added successfully.",
                Data = productModifierList
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

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.ProductModifier entity)
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

            var isValidModifierId = await _context.Modifiers.AnyAsync(x => x.Id == entity.ModifierId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isValidModifierId)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Modifier.";
                response.Data = null;
                return response;
            }

            // Check if the relationship already exists
            var existing = await _context.ProductModifiers
                .FirstOrDefaultAsync(pa => pa.ProductId == entity.ProductId && pa.ModifierId == entity.ModifierId && pa.HeadOfficeId == entity.HeadOfficeId);

            if (existing != null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "This modifier is already associated with the product."
                };
            }

            var productModifier = new Restaurant.Infrastructure.Persistence.Models.ProductModifier
            {
                ProductId = entity.ProductId,
                ModifierId = entity.ModifierId,
                HeadOfficeId = entity.HeadOfficeId,
            };

            await _context.ProductModifiers.AddAsync(productModifier);
            await _context.SaveChangesAsync();

            // Map back to domain entity with generated ID
            entity.Id = productModifier.Id;

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Product modifier added successfully.",
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

    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.ProductModifier entity)
    {
        var response = new ApiResponse();
        try
        {
            var existingEntity = await _context.ProductModifiers
                .FirstOrDefaultAsync(pa => pa.Id == entity.Id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product modifier not found."
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

            var isValidModifierId = await _context.Modifiers.AnyAsync(x => x.Id == entity.ModifierId && x.HeadOfficeId == entity.HeadOfficeId);
            if (!isValidModifierId)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Modifier.";
                response.Data = null;
                return response;
            }


            // Check if the new relationship already exists (for different ID)
            var duplicate = await _context.ProductModifiers
                .FirstOrDefaultAsync(pa => pa.ProductId == entity.ProductId &&
                                         pa.ModifierId == entity.ModifierId &&
                                         pa.HeadOfficeId == entity.HeadOfficeId &&
                                         pa.Id != entity.Id);

            if (duplicate != null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "This modifier is already associated with the product."
                };
            }

            // Update properties
            existingEntity.ProductId = entity.ProductId;
            existingEntity.ModifierId = entity.ModifierId;
            existingEntity.HeadOfficeId = entity.HeadOfficeId;

            _context.ProductModifiers.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifier updated successfully.",
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
            var existingEntity = await _context.ProductModifiers
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product modifier not found."
                };
            }

            _context.ProductModifiers.Remove(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product modifier deleted successfully."
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