using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class HeadOfficeRepository : IHeadOfficeRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public HeadOfficeRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
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

            // Step 1: Check roles for this user
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId!);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = new List<HeadOfficeDto>();
                return apiResponse;
            }

            var query = from h in _context.HeadOffices
                        where !h.IsDeleted
                        join a in _context.Addresses on h.AddressId equals a.AddressId into addr
                        from a in addr.DefaultIfEmpty() // left join for optional address
                        select new HeadOfficeDto
                        {
                            HeadOfficeId = h.HeadOfficeId,
                            Name = h.Name,
                            Description = h.Description,
                            PhoneNumber = h.PhoneNumber,
                            Email = h.Email,
                            Website = h.Website,
                            BusinessCategory = h.BusinessCategory,
                            CreatedAt = h.CreatedAt,
                            CreatedBy = h.CreatedBy,
                            UpdatedAt = h.UpdatedAt,
                            UpdatedBy = h.UpdatedBy,
                            IsActive = h.IsActive,
                            IsDeleted = h.IsDeleted,
                            HeadOfficeAddress = a == null ? null : new AddressDto
                            {
                                AddressId = a.AddressId,
                                AddressLine1 = a.AddressLine1,
                                AddressLine2 = a.AddressLine2,
                                AddressLine3 = a.AddressLine3,
                                City = a.City,
                                State = a.State,
                                Country = a.Country,
                                PostalCode = a.PostalCode,
                                IsDefault = a.IsDefault ?? false
                            }
                        };

            // Step 3: Apply role-based filters
            if (roleCheck.IsSystemAdmin)
            {
                // No filter - system wide access
            }
            else if (roleCheck.IsSuperAdmin)
            {
                var userHeadOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(h => h.HeadOfficeId == userHeadOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                var branchHeadOfficeIds = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                    select b.HeadOfficeId
                ).Distinct().ToListAsync();

                query = query.Where(h => branchHeadOfficeIds.Contains(h.HeadOfficeId));
            }
            else
            {
                // No access if no recognized roles
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User does not have permission to view HeadOffice records.";
                apiResponse.Data = new List<HeadOfficeDto>();
                return apiResponse;
            }

            // Step 4: Execute
            var headOffices = await query.AsNoTracking().ToListAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Records retrieved successfully!";
            apiResponse.Data = headOffices;
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
        var apiResponse = new ApiResponse();

        try
        {
            var userId = _userContext.GetUserId().Value;

            // Step 1: Check roles for this user
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId!);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Step 2: Base query
            var query = from h in _context.HeadOffices
                        where h.HeadOfficeId == id && !h.IsDeleted
                        join a in _context.Addresses on h.AddressId equals a.AddressId into addr
                        from a in addr.DefaultIfEmpty()
                        select new HeadOfficeDto
                        {
                            HeadOfficeId = h.HeadOfficeId,
                            Name = h.Name,
                            Description = h.Description,
                            PhoneNumber = h.PhoneNumber,
                            Email = h.Email,
                            Website = h.Website,
                            BusinessCategory = h.BusinessCategory,
                            CreatedAt = h.CreatedAt,
                            CreatedBy = h.CreatedBy,
                            UpdatedAt = h.UpdatedAt,
                            UpdatedBy = h.UpdatedBy,
                            IsActive = h.IsActive,
                            IsDeleted = h.IsDeleted,
                            HeadOfficeAddress = a == null ? null : new AddressDto
                            {
                                AddressId = a.AddressId,
                                AddressLine1 = a.AddressLine1,
                                AddressLine2 = a.AddressLine2,
                                AddressLine3 = a.AddressLine3,
                                City = a.City,
                                State = a.State,
                                Country = a.Country,
                                PostalCode = a.PostalCode,
                                IsDefault = a.IsDefault ?? false
                            }
                        };

            // Step 3: Apply role-based filters
            if (roleCheck.IsSystemAdmin)
            {
                // No filter - full access
            }
            else if (roleCheck.IsSuperAdmin)
            {
                var userHeadOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(h => h.HeadOfficeId == userHeadOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                var branchHeadOfficeIds = await (
                    from ub in _context.UserBranches
                    join b in _context.Branches on ub.BranchId equals b.BranchId
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                    select b.HeadOfficeId
                ).Distinct().ToListAsync();

                query = query.Where(h => branchHeadOfficeIds.Contains(h.HeadOfficeId));
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User does not have permission to view this HeadOffice record.";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Step 4: Execute query
            var headOffice = await query.AsNoTracking().FirstOrDefaultAsync();

            if (headOffice == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Head office not found or not accessible.";
                apiResponse.Data = null;
                return apiResponse;
            }

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Record retrieved successfully!";
            apiResponse.Data = headOffice;
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


    public async Task<ApiResponse> AddAsync(HeadOfficeDto entity)
    {
        var apiResponse = new ApiResponse();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.GetUserId();

            var address = new Address();

            // Save Address first if provided
            if (entity.HeadOfficeAddress != null)
            {
                address = new Address
                {
                    AddressLine1 = entity.HeadOfficeAddress.AddressLine1,
                    AddressLine2 = entity.HeadOfficeAddress.AddressLine2,
                    AddressLine3 = entity.HeadOfficeAddress.AddressLine3,
                    City = entity.HeadOfficeAddress.City,
                    State = entity.HeadOfficeAddress.State,
                    Country = entity.HeadOfficeAddress.Country,
                    PostalCode = entity.HeadOfficeAddress.PostalCode,
                    IsDefault = entity.HeadOfficeAddress.IsDefault
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                //entity.AddressId = address.AddressId; // link head office to this address
            }

            // Map domain to DB
            var dbEntity = new HeadOffice
            {
                Name = entity.Name,
                Description = entity.Description,
                PhoneNumber = entity.PhoneNumber,
                Email = entity.Email,
                Website = entity.Website,
                AddressId = address.AddressId,
                BusinessCategory = entity.BusinessCategory,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };


            _context.HeadOffices.Add(dbEntity);
            await _context.SaveChangesAsync();

            // Update domain with generated ID
            entity.HeadOfficeId = dbEntity.HeadOfficeId;

            await transaction.CommitAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.Created;
            apiResponse.Message = "Head office created successfully.";
            apiResponse.Data = entity;
            return apiResponse;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
            apiResponse.Data = null;
            return apiResponse;
        }
    }


    public async Task<ApiResponse> UpdateAsync(HeadOfficeDto entity)
    {
        var apiResponse = new ApiResponse();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var userId = _userContext.GetUserId();

            // Check if HeadOffice exists
            var dbEntity = await _context.HeadOffices
                .FirstOrDefaultAsync(x => x.HeadOfficeId == entity.HeadOfficeId && !x.IsDeleted);

            if (dbEntity == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Head office not found.";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Update or create address
            if (entity.HeadOfficeAddress != null)
            {
                if (dbEntity.AddressId.HasValue)
                {
                    // Update existing address
                    var existingAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.AddressId == dbEntity.AddressId);

                    if (existingAddress != null)
                    {
                        existingAddress.AddressLine1 = entity.HeadOfficeAddress.AddressLine1;
                        existingAddress.AddressLine2 = entity.HeadOfficeAddress.AddressLine2;
                        existingAddress.AddressLine3 = entity.HeadOfficeAddress.AddressLine3;
                        existingAddress.City = entity.HeadOfficeAddress.City;
                        existingAddress.State = entity.HeadOfficeAddress.State;
                        existingAddress.Country = entity.HeadOfficeAddress.Country;
                        existingAddress.PostalCode = entity.HeadOfficeAddress.PostalCode;
                        existingAddress.IsDefault = entity.HeadOfficeAddress.IsDefault;

                        _context.Addresses.Update(existingAddress);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Add new address
                    var address = new Address
                    {
                        AddressLine1 = entity.HeadOfficeAddress.AddressLine1,
                        AddressLine2 = entity.HeadOfficeAddress.AddressLine2,
                        AddressLine3 = entity.HeadOfficeAddress.AddressLine3,
                        City = entity.HeadOfficeAddress.City,
                        State = entity.HeadOfficeAddress.State,
                        Country = entity.HeadOfficeAddress.Country,
                        PostalCode = entity.HeadOfficeAddress.PostalCode,
                        IsDefault = entity.HeadOfficeAddress.IsDefault
                    };

                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();
                    dbEntity.AddressId = address.AddressId;
                }
            }

            // Update HeadOffice fields
            dbEntity.Name = entity.Name;
            dbEntity.Description = entity.Description;
            dbEntity.PhoneNumber = entity.PhoneNumber;
            dbEntity.Email = entity.Email;
            dbEntity.Website = entity.Website;
            dbEntity.BusinessCategory = entity.BusinessCategory;
            dbEntity.IsActive = entity.IsActive ?? dbEntity.IsActive;
            dbEntity.UpdatedAt = DateTime.UtcNow;
            dbEntity.UpdatedBy = userId;

            _context.HeadOffices.Update(dbEntity);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Head office updated successfully.";
            apiResponse.Data = entity;
            return apiResponse;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
            apiResponse.Data = null;
            return apiResponse;
        }
    }


    public async Task<ApiResponse> DeleteAsync(int headOfficeId)
    {
        var apiResponse = new ApiResponse();

        try
        {
            var userId = _userContext.GetUserId();

            var dbEntity = await _context.HeadOffices
                .FirstOrDefaultAsync(x => x.HeadOfficeId == headOfficeId && !x.IsDeleted);

            if (dbEntity == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Head office not found.";
                apiResponse.Data = null;
                return apiResponse;
            }

            dbEntity.IsDeleted = true;
            dbEntity.IsActive = false;
            dbEntity.UpdatedAt = DateTime.UtcNow;
            dbEntity.UpdatedBy = userId;

            _context.HeadOffices.Update(dbEntity);
            await _context.SaveChangesAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Head office deleted successfully.";
            apiResponse.Data = new { dbEntity.HeadOfficeId, dbEntity.Name };
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


    // -----------------
    // Mapping Helpers
    // -----------------
    private static Restaurant.Domain.Entities.HeadOffice MapToDomain(HeadOffice db)
    {
        return new Restaurant.Domain.Entities.HeadOffice
        {
            HeadOfficeId = db.HeadOfficeId,
            Name = db.Name,
            Description = db.Description,
            PhoneNumber = db.PhoneNumber,
            Email = db.Email,
            Website = db.Website,
            IsActive = db.IsActive,
            IsDeleted = db.IsDeleted
        };
    }

    private static HeadOffice MapToDb(Restaurant.Domain.Entities.HeadOffice domain)
    {
        return new HeadOffice
        {
            HeadOfficeId = domain.HeadOfficeId != null ? domain.HeadOfficeId.Value : default,
            Name = domain.Name,
            Description = domain.Description,
            PhoneNumber = domain.PhoneNumber,
            Email = domain.Email,
            Website = domain.Website,
            IsActive = domain.IsActive.Value,
            IsDeleted = domain.IsDeleted.Value
        };
    }
}


