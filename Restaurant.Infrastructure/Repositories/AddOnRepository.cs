using Azure;
using Microsoft.EntityFrameworkCore;
using Restaurant.API.Models;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class AddOnRepository : IAddOnRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public AddOnRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
    {
        _context = context;
        _userContext = userContext;
        _baseRepository = baseRepository;
    }

    public async Task<ApiResponse> GetAllAsync(int headOfficeId)
    {
        try
        {
            var addOns = await _context.AddOns
                .Where(a => (a.HeadOfficeId != null && a.HeadOfficeId == headOfficeId) && a.IsDeleted != true)
                .Include(a => a.AddOnCategory)
                .Select(a => new Restaurant.Domain.Entities.AddOnDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    AddOnCategoryId = a.AddOnCategoryId,
                    AddOnUnitPrice = a.AddOnUnitPrice,
                    IsActive = a.IsActive,
                    IsDeleted = a.IsDeleted,
                    HeadOfficeId = a.HeadOfficeId ?? 0,
                    AddOnCategory = new Restaurant.Domain.Entities.AddOnCategoryDto
                    {
                        Id = a.AddOnCategory.Id,
                        Name = a.AddOnCategory.Name,
                        MinSelect = a.AddOnCategory.MinSelect,
                        MaxSelect = a.AddOnCategory.MaxSelect,
                        IsActive = a.AddOnCategory.IsActive,
                        SortOrder = a.AddOnCategory.SortOrder
                    },
                })
                .ToListAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOns retrieved successfully.",
                Data = addOns
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
            var addOn = await _context.AddOns
                .Include(a => a.AddOnCategory)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);

            if (addOn == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOn not found."
                };
            }

            var addOnDto = new Restaurant.Domain.Entities.AddOnDTO
            {
                Id = addOn.Id,
                Name = addOn.Name,
                AddOnCategoryId = addOn.AddOnCategoryId,
                AddOnUnitPrice = addOn.AddOnUnitPrice,
                IsActive = addOn.IsActive,
                IsDeleted = addOn.IsDeleted,
                HeadOfficeId = addOn.HeadOfficeId ?? 0,
                AddOnCategory = new Restaurant.Domain.Entities.AddOnCategoryDto
                {
                    Id = addOn.AddOnCategory.Id,
                    Name = addOn.AddOnCategory.Name,
                    MinSelect = addOn.AddOnCategory.MinSelect,
                    MaxSelect = addOn.AddOnCategory.MaxSelect,
                    IsActive = addOn.AddOnCategory.IsActive,
                    SortOrder = addOn.AddOnCategory.SortOrder
                },
            };

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOn retrieved successfully.",
                Data = addOnDto
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

    public async Task<ApiResponse> AddMultipleAsync(List<Restaurant.Domain.Entities.AddOn> addOns)
    {
        var response = new ApiResponse();

        try
        {
            if (addOns == null || addOns.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No AddOns provided.";
                return response;
            }

            var distinctHoIds = addOns.Select(x => x.HeadOfficeId).Distinct().ToList();
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
            var categoryIds = addOns.Select(a => a.AddOnCategoryId).Distinct().ToList();

            // 2 Validate all categories exist and are active
            var validCategoryIds = await _context.AddOnCategories
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
                response.Message = $"Invalid AddOnCategoryId(s): {string.Join(", ", invalidCategoryIds)}. Please provide valid and active categories.";
                return response;
            }

            // 4 Map valid AddOns to persistence models
            var entities = addOns.Select(a => new Restaurant.Infrastructure.Persistence.Models.AddOn
            {
                Name = a.Name,
                AddOnCategoryId = a.AddOnCategoryId,
                AddOnUnitPrice = a.AddOnUnitPrice,
                IsActive = a.IsActive ?? true,
                IsDeleted = false,
                HeadOfficeId = a.HeadOfficeId,
            }).ToList();

            // 5 Save to database in a single transaction
            await _context.AddOns.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            //// 6 Map generated IDs back to domain entities
            //for (int i = 0; i < addOns.Count; i++)
            //{
            //    addOns[i].Id = entities[i].Id;
            //}

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Message = "AddOns added successfully.";
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


    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.AddOn entity)
    {
        var response = new ApiResponse();
        try
        {
            if (entity == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "No AddOn provided.";
                return response;
            }

            // 1 Validate category existence & integrity
            var category = await _context.AddOnCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.AddOnCategoryId &&
                                          (c.IsActive == true));
            if (category == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid AddOnCategoryId. The specified category does not exist or is inactive.";
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

            // 2 Proceed to add the AddOn
            var addOn = new Restaurant.Infrastructure.Persistence.Models.AddOn
            {
                Name = entity.Name,
                AddOnCategoryId = entity.AddOnCategoryId,
                AddOnUnitPrice = entity.AddOnUnitPrice,
                HeadOfficeId = entity.HeadOfficeId,
                IsActive = entity.IsActive ?? true,
                IsDeleted = false
            };

            await _context.AddOns.AddAsync(addOn);
            await _context.SaveChangesAsync();

            //// 3 Map back to domain entity with generated ID
            //entity.Id = addOn.Id;

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Message = "AddOn added successfully.";
            response.Data = addOn;
            return response;
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = ex.InnerException?.Message ?? ex.Message;
            return response;
        }
    }


    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.AddOn entity)
    {
        var response = new ApiResponse();
        try
        {
            var existingEntity = await _context.AddOns
                .FirstOrDefaultAsync(a => a.Id == entity.Id && a.IsDeleted != true);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOn not found."
                };
            }

            //  Validate category existence & integrity
            var category = await _context.AddOnCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.AddOnCategoryId &&
                                          (c.IsActive == true));

            if (category == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid AddOnCategoryId. The specified category does not exist or is inactive.";
                return response;
            }

            // Update properties
            existingEntity.Name = entity.Name;
            existingEntity.AddOnCategoryId = entity.AddOnCategoryId;
            existingEntity.AddOnUnitPrice = entity.AddOnUnitPrice;
            existingEntity.IsActive = entity.IsActive;

            _context.AddOns.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOn updated successfully.",
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
            var existingEntity = await _context.AddOns
                .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);

            if (existingEntity == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "AddOn not found."
                };
            }

            // Soft delete (set IsDeleted to true)
            existingEntity.IsDeleted = true;
            existingEntity.IsActive = false;

            _context.AddOns.Update(existingEntity);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "AddOn deleted successfully."
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