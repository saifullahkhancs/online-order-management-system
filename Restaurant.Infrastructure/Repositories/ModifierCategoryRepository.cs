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

public class ModifierCategoryRepository : IModifierCategoryRepository
{
    private readonly AppDbContext _context;

    public ModifierCategoryRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
    }

    public async Task<ApiResponse> GetAllAsync(int headOfficeId)
    {
        try
        {
            var modifiers = await _context.ModifierCategories
                .Where(m => (m.HeadOfficeId != null && m.HeadOfficeId == headOfficeId) && m.IsActive == true)
                .Select(m => new Restaurant.Domain.Entities.ModifierCategoryDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    //Price = m.Price,
                    IsRequired = m.IsRequired,
                    IsActive = m.IsActive,
                    HeadOfficeId = m.HeadOfficeId,
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier Categories retrieved successfully.",
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
            var modifiers = await _context.ModifierCategories
                .Where(m => m.Id == id && m.IsActive == true)
                .Select(m => new Restaurant.Domain.Entities.ModifierCategoryDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    //Price = m.Price,
                    IsRequired = m.IsRequired,
                    IsActive = m.IsActive,
                    HeadOfficeId = m.HeadOfficeId,
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier Category retrieved successfully.",
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.ModifierCategory> modifiersCategories)
    {
        var response = new ApiResponse();
        try
        {
            if (modifiersCategories == null || modifiersCategories.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Modifier Category provided.";
                return response;
            }

            var distinctHoIds = modifiersCategories.Select(x => x.HeadOfficeId).Distinct().ToList();
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

            var entities = modifiersCategories.Select(m => new Restaurant.Infrastructure.Persistence.Models.ModifierCategory
            {
                Name = m.Name,
                //Price = m.Price,
                IsRequired = m.IsRequired,
                IsActive = true,
                HeadOfficeId = m.HeadOfficeId
            }).ToList();

            await _context.ModifierCategories.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Modifier Categories added successfully.",
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

    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.ModifierCategory modifiersCategory)
    {
        var response = new ApiResponse();
        try
        {
            if (modifiersCategory == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Modifier Category provided.";
                return response;
            }

            var hoObj = await _context.HeadOffices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeadOfficeId == modifiersCategory.HeadOfficeId && x.IsActive == true);
            if (hoObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeId: {modifiersCategory.HeadOfficeId}. Please provide valid and active HeadOffice.";
                return response;
            }

            // check duplicate Modifier category
            var isDuplicate = await _context.ModifierCategories.AnyAsync(x =>
            x.Name == modifiersCategory.Name &&
            x.HeadOfficeId == modifiersCategory.HeadOfficeId);
            if (isDuplicate == true)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Modifier Category already exists.";
                return response;
            }

            // persist modifier category
            var entities = new Restaurant.Infrastructure.Persistence.Models.ModifierCategory
            {
                Name = modifiersCategory.Name,
                //Price = modifiersCategory.Price,
                IsRequired = modifiersCategory.IsRequired,
                IsActive = true,
                HeadOfficeId = modifiersCategory.HeadOfficeId
            };

            await _context.ModifierCategories.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Message = "Modifier Category added successfully.",
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

    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.ModifierCategory modifiersCategory)
    {
        var response = new ApiResponse();
        try
        {
            if (modifiersCategory == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No Modifier Category provided.";
                return response;
            }

            var existingModifierCategory = await _context.ModifierCategories
                                           .FirstOrDefaultAsync(x => x.Id == modifiersCategory.Id);
            if (existingModifierCategory == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Modifier Category.";
                return response;

            }

            var hoObj = await _context.HeadOffices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeadOfficeId == modifiersCategory.HeadOfficeId && x.IsActive == true);
            if (hoObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeId: {modifiersCategory.HeadOfficeId}. Please provide valid and active HeadOffice.";
                return response;
            }

            // check duplicate Modifier category
            var isDuplicate = await _context.ModifierCategories
                .AnyAsync(x =>  x.Name == modifiersCategory.Name &&
                                x.HeadOfficeId == modifiersCategory.HeadOfficeId &&
                                x.Id != modifiersCategory.Id);
            if (isDuplicate == true)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Modifier Category already exists.";
                return response;
            }

            // persist modifier category
            existingModifierCategory.Name = modifiersCategory.Name;
            //existingModifierCategory.Price = modifiersCategory.Price;
            existingModifierCategory.IsRequired = modifiersCategory.IsRequired;
            existingModifierCategory.IsActive = true;
            existingModifierCategory.HeadOfficeId = modifiersCategory.HeadOfficeId;
            
            _context.ModifierCategories.Update(existingModifierCategory);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Modifier Categories updated successfully.",
                Data = existingModifierCategory
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
            var existingEntity = await _context.ModifierCategories
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Modifier Category not found."
                };
            }

            existingEntity.IsActive = false;

            _context.ModifierCategories.Update(existingEntity);
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