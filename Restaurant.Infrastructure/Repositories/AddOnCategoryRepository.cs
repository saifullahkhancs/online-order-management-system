using Azure;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class AddOnCategoryRepository : IAddOnCategoryRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public AddOnCategoryRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync(int headOfficeId)
    {
        try
        {
            var addOnCategories = await _context.AddOnCategories
                .Where(a => (a.HeadOfficeId != null && a.HeadOfficeId == headOfficeId) && a.IsActive == true)
                .Select(a => new Restaurant.Domain.Entities.AddOnCategoryDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    MinSelect = a.MinSelect,
                    MaxSelect = a.MaxSelect,
                    SortOrder = a.SortOrder,
                    IsActive = a.IsActive,
                    HeadOfficeId = a.HeadOfficeId ?? 0
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOns retrieved successfully.",
                Data = addOnCategories
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
            var addOnCategory = await _context.AddOnCategories
                .FirstOrDefaultAsync(a => a.Id == id);

            if (addOnCategory == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOnCategory not found."
                };
            }

            var addOnCategoryDto = new Restaurant.Domain.Entities.AddOnCategoryDto
            {
                Id = addOnCategory.Id,
                Name = addOnCategory.Name,
                MinSelect   = addOnCategory.MinSelect,
                MaxSelect= addOnCategory.MaxSelect,
                SortOrder = addOnCategory.SortOrder,
                IsActive = addOnCategory.IsActive
            };

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOnCategory retrieved successfully.",
                Data = addOnCategoryDto
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.AddOnCategoryDto> addOnCategories)
    {
        var response = new ApiResponse();

        try
        {
            if (addOnCategories == null || addOnCategories.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No AddOn Categories provided.";
                return response;
            }

            var distinctHoIds = addOnCategories.Select(x => x.HeadOfficeId).Distinct().ToList();
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

            // 4 Map valid AddOns to persistence models
            var entities = addOnCategories.Select(a => new Restaurant.Infrastructure.Persistence.Models.AddOnCategory
            {
                Name = a.Name,
                MinSelect   = a.MinSelect,
                MaxSelect = a.MaxSelect,
                IsActive = a.IsActive,
                SortOrder = a.SortOrder,
                HeadOfficeId = a.HeadOfficeId,
            }).ToList();

            // 5 Save to database in a single transaction
            await _context.AddOnCategories.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            //// 6 Map generated IDs back to domain entities
            //for (int i = 0; i < addOnCategories.Count; i++)
            //{
            //    addOnCategories[i].Id = entities[i].Id;
            //}

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Message = "AddOnCategories added successfully.";
            response.Data = entities;
            return response;
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = ex.InnerException?.Message ?? ex.Message;
            return response;
        }
    }


    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.AddOnCategoryDto entity)
    {
        var response = new ApiResponse();
        try
        {
            if (entity == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No AddOn Category provided.";
                return response;
            }

            var hoObj = await _context.HeadOffices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeadOfficeId == entity.HeadOfficeId && x.IsActive == true);
            if (hoObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeId: {entity.HeadOfficeId}. Please provide valid and active HeadOffice.";
                return response;
            }

            // check duplicate Modifier category
            var isDuplicate = await _context.AddOnCategories.AnyAsync(x =>
            x.Name == entity.Name &&
            x.HeadOfficeId == entity.HeadOfficeId);
            if (isDuplicate == true)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"AddOn Category already exists.";
                return response;
            }

            // 2 Proceed to add the AddOn
            var addOnCategory = new Restaurant.Infrastructure.Persistence.Models.AddOnCategory
            {
                Name = entity.Name,
                MinSelect = entity.MinSelect,
                MaxSelect = entity.MaxSelect,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                HeadOfficeId = entity.HeadOfficeId,
            };

            await _context.AddOnCategories.AddAsync(addOnCategory);
            await _context.SaveChangesAsync();

            //// 3 Map back to domain entity with generated ID
            //entity.Id = addOnCategory.Id;

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Message = "AddOnCategory added successfully.";
            response.Data = addOnCategory;
            return response;
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = ex.InnerException?.Message ?? ex.Message;
            return response;
        }
    }


    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.AddOnCategoryDto entity)
    {
        ApiResponse response = new ApiResponse();
        try
        {
            if (entity == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No AddOn Category provided.";
                return response;
            }

            var existingEntity = await _context.AddOnCategories
                .FirstOrDefaultAsync(a => a.Id == entity.Id);
            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOn Category not found."
                };
            }

            // Validate HeadOffice Id
            var hoObj = await _context.HeadOffices
               .AsNoTracking()
               .FirstOrDefaultAsync(x => x.HeadOfficeId == entity.HeadOfficeId && x.IsActive == true);
            if (hoObj == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"Invalid HeadOfficeId: {entity.HeadOfficeId}. Please provide valid and active HeadOffice.";
                return response;
            }

            // check duplicate Modifier category
            var isDuplicate = await _context.ModifierCategories
                .AnyAsync(x => x.Name == entity.Name &&
                                x.HeadOfficeId == entity.HeadOfficeId &&
                                x.Id != entity.Id);
            if (isDuplicate == true)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = $"AddOn Category already exists.";
                return response;
            }

            // Update properties
            existingEntity.Name = entity.Name;
            existingEntity.MinSelect = entity.MinSelect;
            existingEntity.MaxSelect = entity.MaxSelect;
            existingEntity.SortOrder = entity.SortOrder;
            existingEntity.IsActive = entity.IsActive;
            existingEntity.HeadOfficeId = entity.HeadOfficeId;

            _context.AddOnCategories.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOn Category updated successfully.",
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
            var existingEntity = await _context.AddOnCategories
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOnCategory not found."
                };
            }

            // Soft delete (set IsDeleted to true)
            existingEntity.IsActive = false;

            _context.AddOnCategories.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOnCategory deleted successfully."
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