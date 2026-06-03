using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;

public class BranchRepository : IBranchRepository
{
    private readonly AppDbContext _context;
    private readonly UserContextService _userContext;
    private readonly IBaseRepository _baseRepository;

    public BranchRepository(AppDbContext context, UserContextService userContext,IBaseRepository baseRepository)
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

            // Step 1: Get role checks
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId!);
            var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

            if (roleCheck == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Unauthorized User";
                apiResponse.Data = new List<BranchDto>();
                return apiResponse;
            }

            // Step 2: Base query
            var query = from b in _context.Branches
                        where !b.IsDeleted
                        join h in _context.HeadOffices on b.HeadOfficeId equals h.HeadOfficeId into ho
                        from h in ho.DefaultIfEmpty()
                        join a in _context.Addresses on b.AddressId equals a.AddressId into addr
                        from a in addr.DefaultIfEmpty()
                        select new BranchDto
                        {
                            HeadOfficeId = b.HeadOfficeId ?? 0,
                            HeadOfficeName = h == null ? null : h.Name,
                            BranchId = b.BranchId,
                            BranchName = b.BranchName,
                            Email = b.Email,
                            PhoneNumber = b.PhoneNumber,
                            Latitude = b.Latitude,
                            Longitude = b.Longitude,
                            IsActive = b.IsActive,
                            IsDeleted = b.IsDeleted,
                            CreatedAt = b.CreatedAt,
                            CreatedBy = b.CreatedBy,
                            UpdatedAt = b.UpdatedAt,
                            UpdatedBy = b.UpdatedBy,
                            BranchAddress = a == null ? null : new AddressDto
                            {
                                AddressId = a.AddressId,
                                AddressLine1 = a.AddressLine1,
                                AddressLine2 = a.AddressLine2,
                                AddressLine3 = a.AddressLine3,
                                City = a.City,
                                Country = a.Country,
                                PostalCode = a.PostalCode
                            },
                            BranchTimings = _context.BranchTimings
                                .Where(t => t.BranchId == b.BranchId)
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
                        };

            // Step 3: Apply role-based filters
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin → Full system visibility (no filter applied)
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin → Only branches of their HeadOffice
                var userHeadOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(b => b.HeadOfficeId == userHeadOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin & OrderTaker → Only their assigned branches
                var branchIds = await (
                    from ub in _context.UserBranches
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                    select ub.BranchId
                ).Distinct().ToListAsync();

                query = query.Where(b => branchIds.Contains(b.BranchId));
            }
            else
            {
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User does not have permission to view Branch records.";
                apiResponse.Data = new List<BranchDto>();
                return apiResponse;
            }

            // Step 4: Execute
            var branches = await query.AsNoTracking().ToListAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Records retrieved successfully!";
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
            var query = from b in _context.Branches
                        where !b.IsDeleted && b.BranchId == id
                        join h in _context.HeadOffices on b.HeadOfficeId equals h.HeadOfficeId into ho
                        from h in ho.DefaultIfEmpty()
                        join a in _context.Addresses on b.AddressId equals a.AddressId into addr
                        from a in addr.DefaultIfEmpty()
                        select new BranchDto
                        {
                            HeadOfficeId = b.HeadOfficeId ?? 0,
                            HeadOfficeName = h == null ? null : h.Name,
                            BranchId = b.BranchId,
                            BranchName = b.BranchName,
                            Email = b.Email,
                            PhoneNumber = b.PhoneNumber,
                            Latitude = b.Latitude,
                            Longitude = b.Longitude,
                            IsActive = b.IsActive,
                            IsDeleted = b.IsDeleted,
                            CreatedAt = b.CreatedAt,
                            CreatedBy = b.CreatedBy,
                            UpdatedAt = b.UpdatedAt,
                            UpdatedBy = b.UpdatedBy,
                            BranchAddress = a == null ? null : new AddressDto
                            {
                                AddressId = a.AddressId,
                                AddressLine1 = a.AddressLine1,
                                AddressLine2 = a.AddressLine2,
                                AddressLine3 = a.AddressLine3,
                                City = a.City,
                                Country = a.Country,
                                PostalCode = a.PostalCode
                            },
                            BranchTimings = _context.BranchTimings
                                .Where(t => t.BranchId == b.BranchId)
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
                        };

            // Step 3: Apply role-based filters
            if (roleCheck.IsSystemAdmin)
            {
                // SystemAdmin → full access (no filter needed)
            }
            else if (roleCheck.IsSuperAdmin)
            {
                // SuperAdmin → only branches of their HeadOffice
                var userHeadOfficeId = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HeadOfficeId)
                    .FirstOrDefaultAsync();

                query = query.Where(b => b.HeadOfficeId == userHeadOfficeId);
            }
            else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
            {
                // Admin / OrderTaker → only assigned branches
                var branchIds = await (
                    from ub in _context.UserBranches
                    where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                    select ub.BranchId
                ).Distinct().ToListAsync();

                query = query.Where(b => branchIds.Contains(b.BranchId));
            }
            else
            {
                // No recognized role → deny
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User does not have permission to view this branch.";
                apiResponse.Data = null;
                return apiResponse;
            }

            // Step 4: Execute
            var branch = await query.AsNoTracking().FirstOrDefaultAsync();

            if (branch == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Branch not found or not accessible.";
                apiResponse.Data = null;
                return apiResponse;
            }

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Record retrieved successfully!";
            apiResponse.Data = branch;
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



    public async Task<ApiResponse> AddAsync(Restaurant.Domain.Entities.Branch entity)
    {
        var apiResponse = new ApiResponse();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate HeadOffice existence
            var headOfficeExists = await _context.HeadOffices
                .AnyAsync(h => h.HeadOfficeId == entity.HeadOfficeId && !h.IsDeleted);

            if (!headOfficeExists)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                apiResponse.Data = null;
                return apiResponse;
            }

            var userId = _userContext.GetUserId();

            // Save Address first if provided
            if (entity.BranchAddress != null)
            {
                var address = new Address
                {
                    AddressLine1 = entity.BranchAddress.AddressLine1,
                    AddressLine2 = entity.BranchAddress.AddressLine2,
                    AddressLine3 = entity.BranchAddress.AddressLine3,
                    City = entity.BranchAddress.City,
                    State = entity.BranchAddress.State,
                    Country = entity.BranchAddress.Country,
                    PostalCode = entity.BranchAddress.PostalCode,
                    IsDefault = entity.BranchAddress.IsDefault
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                entity.AddressId = address.AddressId; // link branch to this address
            }

            var dbEntity = MapToDb(entity);
            dbEntity.BranchId = 0; // ensure new insert
            dbEntity.CreatedBy = userId;
            dbEntity.CreatedAt = DateTime.UtcNow;
            dbEntity.IsActive = true;
            dbEntity.IsDeleted = false;

            _context.Branches.Add(dbEntity);
            await _context.SaveChangesAsync();

            // update domain with generated ID
            entity.BranchId = dbEntity.BranchId;

            // Save BranchTimings
            if (entity.BranchTimings != null && entity.BranchTimings.Any())
            {
                foreach (var timing in entity.BranchTimings)
                {
                    var branchTimingObj = new BranchTiming
                    {
                        BranchId = dbEntity.BranchId,
                        BranchTimingName = timing.BranchTimingName,
                        OpenTime = timing.OpenTime,
                        CloseTime = timing.CloseTime,
                        SortOrder = timing.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        IsActive = true,
                        IsClosed = false
                    };

                    _context.BranchTimings.Add(branchTimingObj);
                }
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.Created;
            apiResponse.Message = "Branch created successfully.";
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


    public async Task<ApiResponse> UpdateAsync(Restaurant.Domain.Entities.Branch entity)
    {
        var apiResponse = new ApiResponse();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if branch exists
            var dbEntity = await _context.Branches
                .FirstOrDefaultAsync(x => x.BranchId == entity.BranchId && !x.IsDeleted);

            if (dbEntity == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Branch not found.";
                return apiResponse;
            }

            // Validate HeadOffice existence
            var headOfficeExists = await _context.HeadOffices
                .AnyAsync(h => h.HeadOfficeId == entity.HeadOfficeId && !h.IsDeleted);

            if (!headOfficeExists)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                return apiResponse;
            }

            var userId = _userContext.GetUserId();

            // --- Update or Insert Address ---
            if (entity.BranchAddress != null)
            {
                if (dbEntity.AddressId.HasValue)
                {
                    // update existing address
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.AddressId == dbEntity.AddressId);

                    if (address != null)
                    {
                        address.AddressLine1 = entity.BranchAddress.AddressLine1;
                        address.AddressLine2 = entity.BranchAddress.AddressLine2;
                        address.AddressLine3 = entity.BranchAddress.AddressLine3;
                        address.City = entity.BranchAddress.City;
                        address.State = entity.BranchAddress.State;
                        address.Country = entity.BranchAddress.Country;
                        address.PostalCode = entity.BranchAddress.PostalCode;
                        address.IsDefault = entity.BranchAddress.IsDefault;
                        _context.Addresses.Update(address);
                    }
                }
                else
                {
                    // add new address
                    var newAddress = new Address
                    {
                        AddressLine1 = entity.BranchAddress.AddressLine1,
                        AddressLine2 = entity.BranchAddress.AddressLine2,
                        AddressLine3 = entity.BranchAddress.AddressLine3,
                        City = entity.BranchAddress.City,
                        State = entity.BranchAddress.State,
                        Country = entity.BranchAddress.Country,
                        PostalCode = entity.BranchAddress.PostalCode,
                        IsDefault = entity.BranchAddress.IsDefault
                    };

                    _context.Addresses.Add(newAddress);
                    await _context.SaveChangesAsync();
                    dbEntity.AddressId = newAddress.AddressId;
                }
            }

            // --- Update branch fields ---
            dbEntity.HeadOfficeId = entity.HeadOfficeId;
            dbEntity.BranchName = entity.BranchName;
            dbEntity.PhoneNumber = entity.PhoneNumber;
            dbEntity.Email = entity.Email;
            dbEntity.Latitude = entity.Latitude;
            dbEntity.Longitude = entity.Longitude;
            dbEntity.IsActive = entity.IsActive;
            dbEntity.UpdatedAt = DateTime.UtcNow;
            dbEntity.UpdatedBy = userId;

            _context.Branches.Update(dbEntity);
            await _context.SaveChangesAsync();

            // --- Update BranchTimings ---
            if (entity.BranchTimings != null && entity.BranchTimings.Any())
            {
                // remove old timings
                var existingTimings = await _context.BranchTimings
                    .Where(bt => bt.BranchId == dbEntity.BranchId)
                    .ToListAsync();

                if (existingTimings.Any())
                    _context.BranchTimings.RemoveRange(existingTimings);

                // add new timings
                foreach (var timing in entity.BranchTimings)
                {
                    var branchTimingObj = new BranchTiming
                    {
                        BranchId = dbEntity.BranchId,
                        BranchTimingName = timing.BranchTimingName,
                        OpenTime = timing.OpenTime,
                        CloseTime = timing.CloseTime,
                        SortOrder = timing.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        IsActive = true,
                        IsClosed = timing.IsClosed
                    };

                    _context.BranchTimings.Add(branchTimingObj);
                }
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Branch updated successfully.";
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



    public async Task<ApiResponse> DeleteAsync(int Id)
    {
        var apiResponse = new ApiResponse();

        try
        {
            var dbEntity = await _context.Branches
                .FirstOrDefaultAsync(x => x.BranchId == Id && !x.IsDeleted);

            if (dbEntity == null)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Branch not found or already deleted.";
                apiResponse.Data = null;
                return apiResponse;
            }

            var userId = _userContext.GetUserId();

            // Soft delete
            dbEntity.IsDeleted = true;
            dbEntity.IsActive = false;
            dbEntity.UpdatedAt = DateTime.UtcNow;
            dbEntity.UpdatedBy = userId;

            _context.Branches.Update(dbEntity);
            await _context.SaveChangesAsync();

            apiResponse.StatusCode = (int)HttpStatusCode.OK;
            apiResponse.Message = "Branch deleted successfully.";
            apiResponse.Data = null;
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
    private static Restaurant.Domain.Entities.Branch MapToDomain(Branch db)
    {
        return new Restaurant.Domain.Entities.Branch
        {
            BranchId = db.BranchId,
            HeadOfficeId = db.HeadOfficeId.Value,
            BranchName = db.BranchName,
            AddressId = db.AddressId,
            PhoneNumber = db.PhoneNumber,
            Email = db.Email,
            Latitude = db.Latitude,
            Longitude = db.Longitude,
            IsActive = db.IsActive,
            IsDeleted = db.IsDeleted
        };
    }

    private static Branch MapToDb(Restaurant.Domain.Entities.Branch domain)
    {
        return new Branch
        {
            BranchId = domain.BranchId,
            HeadOfficeId = domain.HeadOfficeId,
            BranchName = domain.BranchName,
            AddressId = domain.AddressId,
            PhoneNumber = domain.PhoneNumber,
            Email = domain.Email,
            Latitude = domain.Latitude,
            Longitude = domain.Longitude,
            IsActive = domain.IsActive,
            IsDeleted = domain.IsDeleted
        };
    }
}


