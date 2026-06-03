using Azure;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

public class ModifierRepository : IModifierRepository
{
    private readonly AppDbContext _context;

    public ModifierRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
    }

    public async Task<ApiResponse> GetAllAsync(int headOfficeId)
    {
        try
        {
            var modifiers = await _context.Modifiers
                .Include(m => m.Category)
                .Where(m => (m.HeadOfficeId != null && m.HeadOfficeId == headOfficeId) && m.IsDeleted == false)
                .Select(m => new Restaurant.Domain.Entities.ModifierDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    DefaultPrice = m.DefaultPrice,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category.Name,
                    HeadOfficeId = m.HeadOfficeId,
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifiers retrieved successfully.",
                Data = modifiers
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
            var modifier = await _context.Modifiers
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);

            if (modifier == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Modifier not found."
                };
            }

            var modifierDto = new Restaurant.Domain.Entities.ModifierDTO
            {
                Id = modifier.Id,
                Name = modifier.Name,
                DefaultPrice = modifier.DefaultPrice,
                CategoryId = modifier.CategoryId,
                CategoryName = modifier.Category.Name,
                HeadOfficeId = modifier.HeadOfficeId,
            };

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier retrieved successfully.",
                Data = modifierDto
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.Modifier> modifiers)
    {
        var response = new ApiResponse();
        try
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Modifiers provided.";
                return response;
            }

            var distinctHoIds = modifiers.Select(x => x.HeadOfficeId).Distinct().ToList();
            var validHOIds = await _context.HeadOffices
                .AsNoTracking()
                .Where(x => distinctHoIds.Contains(x.HeadOfficeId) && x.IsActive == true)
                .Select(x => x.HeadOfficeId).ToListAsync();
            var invalidHOIds = distinctHoIds.Except(validHOIds).ToList();
            if (invalidHOIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeIds(s): {string.Join(", ", invalidHOIds)}. Please provide valid and active HeadOffices.";
                return response;
            }


            // 1 Collect distinct category IDs from input
            var categoryIds = modifiers.Select(a => a.CategoryId).Distinct().ToList();

            // 2 Validate all categories exist and are active
            var validCategoryIds = await _context.ModifierCategories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id) &&
                            (c.IsActive == true))
                .Select(c => c.Id)
                .ToListAsync();

            // 3 Identify invalid category IDs
            var invalidCategoryIds = categoryIds.Except(validCategoryIds).ToList();
            if (invalidCategoryIds.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid Modifier CategoryId(s): {string.Join(", ", invalidCategoryIds)}. Please provide valid and active categories.";
                return response;
            }

            var entities = modifiers.Select(m => new Restaurant.Infrastructure.Persistence.Models.Modifier
            {
                Name = m.Name,
                CategoryId = m.CategoryId,
                DefaultPrice = m.DefaultPrice,
                HeadOfficeId = m.HeadOfficeId,
                IsActive = true,
                IsDeleted = false
            }).ToList();

            await _context.Modifiers.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Modifiers added successfully.",
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

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.Modifier modifier)
    {
        var response = new ApiResponse();
        try
        {
            if (modifier == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Modifiers provided.";
                return response;
            }

            var hoObj = await _context.HeadOffices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeadOfficeId == modifier.HeadOfficeId && x.IsActive == true);
            
            if (hoObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeId: {modifier.HeadOfficeId}. Please provide valid and active HeadOffice.";
                return response;
            }

            // Validate category
            var catObj = await _context.ModifierCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == modifier.CategoryId && (c.IsActive == true));

            if (catObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid Modifier CategoryId: { modifier.CategoryId}. Please provide valid and active category.";
                return response;
            }

            var entities = new Restaurant.Infrastructure.Persistence.Models.Modifier
            {
                Name = modifier.Name,
                CategoryId = modifier.CategoryId,
                DefaultPrice = modifier.DefaultPrice,
                HeadOfficeId = modifier.HeadOfficeId,
                IsActive = true,
                IsDeleted = false
            };

            await _context.Modifiers.AddAsync(entities);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Modifier added successfully.",
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

    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.Modifier entity)
    {
        var response = new ApiResponse();
        try
        {
            var existingEntity = await _context.Modifiers
                .FirstOrDefaultAsync(m => m.Id == entity.Id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Modifier not found."
                };
            }

            // Validate category
            var catObj = await _context.ModifierCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CategoryId && (c.IsActive == true));

            if (catObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid Modifier CategoryId: {entity.CategoryId}. Please provide valid and active category.";
                return response;
            }

            // Update properties
            existingEntity.Name = entity.Name;
            existingEntity.CategoryId = entity.CategoryId;
            existingEntity.DefaultPrice = entity.DefaultPrice;
            existingEntity.IsActive = entity.IsActive;

            _context.Modifiers.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier updated successfully.",
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
            var existingEntity = await _context.Modifiers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Modifier not found."
                };
            }

            existingEntity.IsActive = false;
            existingEntity.IsDeleted = true;

            _context.Modifiers.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier deleted successfully."
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