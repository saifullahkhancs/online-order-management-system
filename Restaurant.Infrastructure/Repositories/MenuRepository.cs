using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Net;
public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _context;

    public MenuRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse> GetMenu(int branchId)
    {
        try
        {
            // get all those prodducts specific to that branch id
            // use the branchProducts table to get the products for that branch
            // grpup all the products based on category
            var menu = await _context.BranchProducts
               .Where(bp => bp.BranchId == branchId && bp.IsActive)
               .Include(bp => bp.Product)
                   .ThenInclude(p => p.CategoryProducts)
                       .ThenInclude(cp => cp.Category)
               .Select(bp => bp.Product)
               .Where(p => !p.IsDeleted)
               .ToListAsync();



            var groupedMenu = menu
            .Where(p => p.CategoryProducts?.Any() == true)
            .GroupBy(p => p.CategoryProducts.First().Category)
            .Select(g => new
            {
                Category = new CategoryDTO
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Description = g.Key.Description,
                    ImageUrl = g.Key.ImageUrl,
                    Products = g.Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Description = p.Description,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        IsAvailable = p.IsAvailable
                    }).ToList()
                }
            })
            .ToList();


            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Menu retrieved successfully.",
                Data = groupedMenu
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


    public async Task<ApiResponse> GetAllRestaurantBranchesAsync(int restaurantId)
    {
        var apiResponse = new ApiResponse();

        try
        {
            // Step 1: Verify restaurant exists
            var restaurantExists = await _context.HeadOffices
                .AnyAsync(r => r.HeadOfficeId == restaurantId && !r.IsDeleted && r.IsActive);

            if (!restaurantExists)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Restaurant not found or inactive.";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Step 2: Query branches for that restaurant
            var branches = await (from b in _context.Branches
                                  join h in _context.HeadOffices on b.HeadOfficeId equals h.HeadOfficeId into ho
                                  from h in ho.DefaultIfEmpty()
                                  join a in _context.Addresses on b.AddressId equals a.AddressId into addr
                                  from a in addr.DefaultIfEmpty()
                                  where !b.IsDeleted && b.IsActive && h.HeadOfficeId == restaurantId
                                  select new BranchDto
                                  {
                                      BranchId = b.BranchId,
                                      BranchName = b.BranchName,
                                      Email = b.Email,
                                      PhoneNumber = b.PhoneNumber,
                                      Latitude = b.Latitude,
                                      Longitude = b.Longitude,
                                      IsActive = b.IsActive,
                                      IsDeleted = b.IsDeleted,
                                      HeadOfficeId = b.HeadOfficeId ?? 0,
                                      HeadOfficeName = h == null ? null : h.Name,
                                      HeadOffice = h == null ? null : new HeadOfficeDto
                                      {
                                          HeadOfficeId = h.HeadOfficeId,
                                          Name = h.Name,
                                          Description = h.Description,
                                          PhoneNumber = h.PhoneNumber,
                                          Email = h.Email,
                                          Website = h.Website,
                                          BusinessCategory = h.BusinessCategory
                                      },
                                      BranchAddress = a == null ? null : new AddressDto
                                      {
                                          AddressId = a.AddressId,
                                          AddressLine1 = a.AddressLine1,
                                          AddressLine2 = a.AddressLine2,
                                          AddressLine3 = a.AddressLine3,
                                          City = a.City,
                                          State = a.State,
                                          Country = a.Country,
                                          PostalCode = a.PostalCode,
                                      },
                                      BranchTimings = _context.BranchTimings
                                          .Where(t => t.BranchId == b.BranchId && t.IsActive)
                                          .OrderBy(t => t.SortOrder)
                                          .Select(t => new BranchTimingDto
                                          {
                                              BranchTimingId = t.BranchTimingId,
                                              BranchTimingName = t.BranchTimingName,
                                              OpenTime = t.OpenTime,
                                              CloseTime = t.CloseTime,
                                              IsClosed = t.IsClosed,
                                              SortOrder = t.SortOrder,
                                              IsActive = t.IsActive
                                          }).ToList()
                                  })
                                  .AsNoTracking()
                                  .ToListAsync();

            // Step 3: Return response
            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = branches.Any()
                ? "Branches retrieved successfully."
                : "No active branches found for this restaurant.";
            apiResponse.Data = branches;
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

    public async Task<ApiResponse> GetProductDetails(int productId)
    {
        try
        {
            // Get product with modifiers and add-ons details
            var product = await _context.Products
                .Where(p => p.ProductId == productId && !p.IsDeleted)
                .Include(p => p.CategoryProducts)
                    .ThenInclude(cp => cp.Category)
                .Include(p => p.ProductModifiers)
                    .ThenInclude(pm => pm.Modifier)
                        .ThenInclude(m => m.Category)
                .Include(p => p.ProductAddOns)
                    .ThenInclude(pa => pa.AddOn)
                        .ThenInclude(a => a.AddOnCategory)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return new ApiResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Product not found."
                };
            }

            // Group modifiers by category
            var modifierGroups = product.ProductModifiers
                .Where(pm => pm.Modifier != null && pm.Modifier.Category != null)
                .GroupBy(pm => pm.Modifier.Category)
                .Select(g => new
                {
                    Category = new ModifierCategoryDTO
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        IsRequired = g.Key.IsRequired ?? false
                    },
                    Modifiers = g.Select(pm => new ModifierDTO
                    {
                        Id = pm.Modifier.Id,
                        Name = pm.Modifier.Name,
                        CategoryId = pm.Modifier.CategoryId,
                        DefaultPrice = pm.Modifier.DefaultPrice
                    }).ToList()
                })
                .ToList();

            // Group add-ons by category
            var addOnGroups = product.ProductAddOns
                .Where(pa => pa.AddOn != null && pa.AddOn.AddOnCategory != null)
                .GroupBy(pa => pa.AddOn.AddOnCategory)
                .Select(g => new
                {
                    Category = new AddOnCategoryDTO
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        MinSelect = g.Key.MinSelect,
                        MaxSelect = g.Key.MaxSelect,
                        SortOrder = g.Key.SortOrder
                    },
                    AddOns = g.Select(pa => new AddOnDTO
                    {
                        Id = pa.AddOn.Id,
                        Name = pa.AddOn.Name,
                        AddOnCategoryId = pa.AddOn.AddOnCategoryId,
                        AddOnUnitPrice = pa.AddOn.AddOnUnitPrice,
                        IsActive = pa.AddOn.IsActive
                    }).ToList()
                })
                .OrderBy(g => g.Category.SortOrder).ToList();

            // Get product categories
            var productCategories = product.CategoryProducts?
                .Where(cp => cp.Category != null)
                .Select(cp => new CategoryDTO
                {
                    CategoryId = cp.Category.CategoryId,
                    CategoryName = cp.Category.CategoryName,
                    Description = cp.Category.Description,
                    ImageUrl = cp.Category.ImageUrl
                })
                .ToList() ?? new List<CategoryDTO>();

            return new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Product details retrieved successfully.",
                Data = new
                {
                    Product = new ProductDTO
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Description = product.Description,
                        Price = product.Price,
                        ImageUrl = product.ImageUrl,
                        IsAvailable = product.IsAvailable,
                        Categories = productCategories
                    },
                    ModifierGroups = modifierGroups,
                    AddOnGroups = addOnGroups
                }
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