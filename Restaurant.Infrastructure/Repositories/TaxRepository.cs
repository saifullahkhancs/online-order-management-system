using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Infrastructure.Repositories
{
    public class TaxRepository : ITaxRepository
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContext;
        private readonly IBaseRepository _baseRepository;

        public TaxRepository(AppDbContext context, UserContextService userContext, IBaseRepository baseRepository)
        {
            _context = context;
            _userContext = userContext;
            _baseRepository = baseRepository;
        }

        // ================== READ ==================

        public async Task<ApiResponse> GetAllAsync()
        {
            var apiResponse = new ApiResponse();
            try
            {
                var userId = _userContext.GetUserId().Value;
                var roleCheck = await GetRoleCheck(userId);
                if (roleCheck == null)
                    return UnauthorizedResponse("Unauthorized User");

                var query = _context.Taxs.Where(t => !(t.IsDeleted.HasValue && t.IsDeleted.Value == true));

                if (roleCheck.IsSystemAdmin)
                {
                    // no filter
                }
                else if (roleCheck.IsSuperAdmin)
                {
                    var headOfficeId = await _context.Users
                        .Where(u => u.UserId == userId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    var branchIds = await _context.Branches
                        .Where(b => b.HeadOfficeId == headOfficeId && !b.IsDeleted)
                        .Select(b => b.BranchId)
                        .ToListAsync();

                    query = from t in query
                            join bt in _context.BranchTaxes on t.TaxId equals bt.TaxId
                            where branchIds.Contains(bt.BranchId.Value)
                            select t;
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {
                    var branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == userId && (ub.IsActive == null || ub.IsActive == true) && (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();

                    query = from t in query
                            join bt in _context.BranchTaxes on t.TaxId equals bt.TaxId
                            where branchIds.Contains(bt.BranchId.Value)
                            select t;
                }
                else
                {
                    return ForbiddenResponse("User does not have permission to view Tax records.");
                }

                //var taxes = await query.AsNoTracking().Distinct().ToListAsync();
                var taxes = await (
                                    from t in query
                                    select new
                                    {
                                        t.TaxId,
                                        t.Code,
                                        t.Name,
                                        t.Rate,
                                        t.IsPercentage,
                                        t.IsCompound,
                                        t.Priority,
                                        t.CreatedAt,
                                        t.CreatedBy,
                                        t.UpdatedAt,
                                        t.UpdatedBy,
                                        t.IsActive,
                                        t.PaymentTypeId,
                                        t.PaymentType,
                                        t.HeadOfficeId,
                                        Branches = (
                                            from bt in _context.BranchTaxes
                                            join b in _context.Branches on bt.BranchId equals b.BranchId
                                            where bt.TaxId == t.TaxId && !b.IsDeleted
                                            select new
                                            {
                                                b.BranchId,
                                                b.BranchName
                                            }
                                        ).ToList()
                                    }
                                )
                                .AsNoTracking()
                                //.Distinct() // optional, safe to include
                                .ToListAsync();
                return SuccessResponse("Taxes retrieved successfully.", taxes);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex);
            }
        }

        public async Task<ApiResponse> GetByIdAsync(int id)
        {
            try
            {
                var allTaxesResponse = await GetAllAsync();
                if (allTaxesResponse.StatusCode != (int)HttpStatusCode.OK)
                    return allTaxesResponse;

                var taxes = allTaxesResponse.Data as List<Tax>;
                var tax = taxes?.FirstOrDefault(t => t.TaxId == id);

                if (tax == null)
                    return NotFoundResponse("Tax not found.");

                return SuccessResponse("Tax retrieved successfully.", tax);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex);
            }
        }

        // ================== CREATE ==================

        public async Task<ApiResponse> CreateAsync(CreateTaxRequest dto)
        {
            var apiResponse = new ApiResponse();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate logged-in user
                var userId = _userContext.GetUserId();
                if (!userId.HasValue)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    apiResponse.Message = "Unauthorized user.";
                    return apiResponse;
                }

                // Role check (only SuperAdmin allowed)
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId.Value);
                var roleCheck = roleCheckResponse?.Data as BaseRepository.RoleCheckResult;
                if (roleCheck == null || !roleCheck.IsSuperAdmin)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                    apiResponse.Message = "Only SuperAdmin can create taxes.";
                    return apiResponse;
                }

                // Validate HeadOfficeId if provided
                if (dto.HeadOfficeId.HasValue)
                {
                    var headOfficeExists = await _context.HeadOffices
                        .AnyAsync(h => h.HeadOfficeId == dto.HeadOfficeId.Value && !h.IsDeleted);
                    if (!headOfficeExists)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                        return apiResponse;
                    }
                }

                // Optional: Prevent duplicate tax code for same head office (helpful)
                var duplicateTax = await _context.Taxs
                    .AnyAsync(t => t.Code == dto.Code && (dto.HeadOfficeId == null || t.HeadOfficeId == dto.HeadOfficeId));
                if (duplicateTax)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "Tax with the same code already exists for the specified HeadOffice.";
                    return apiResponse;
                }

                // Create Tax
                var tax = new Tax
                {
                    Code = dto.Code?.Trim(),
                    Name = dto.Name?.Trim(),
                    Rate = dto.Rate,
                    IsPercentage = dto.IsPercentage,
                    IsCompound = dto.IsCompound,
                    Priority = dto.Priority,
                    PaymentTypeId = dto.PaymentTypeId,
                    PaymentType = dto.PaymentType,
                    HeadOfficeId = dto.HeadOfficeId.Value,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value,
                    IsActive = true,
                    IsDeleted = false
                };

                _context.Taxs.Add(tax);
                await _context.SaveChangesAsync();

                // If branch list provided, validate branches and map tax -> branches
                if (dto.BranchIds != null && dto.BranchIds.Any())
                {
                    var requestedBranchIds = dto.BranchIds.Distinct().ToList();

                    // validate branch ids: exist, not deleted, and (if HeadOfficeId provided) belong to that HeadOffice
                    var validBranchIds = await _context.Branches
                        .Where(b => requestedBranchIds.Contains(b.BranchId)
                                    && !b.IsDeleted
                                    && (!dto.HeadOfficeId.HasValue || b.HeadOfficeId == dto.HeadOfficeId))
                        .Select(b => b.BranchId)
                        .ToListAsync();

                    var invalidBranchIds = requestedBranchIds.Except(validBranchIds).ToList();
                    if (invalidBranchIds.Any())
                    {
                        await transaction.RollbackAsync();
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = $"One or more branchIds are invalid or not allowed: {string.Join(',', invalidBranchIds)}";
                        apiResponse.Data = null;
                        return apiResponse;
                    }

                    var branchTaxes = validBranchIds.Select(bid => new BranchTaxis
                    {
                        BranchId = bid,
                        TaxId = tax.TaxId,
                        Priority = dto.Priority,
                        IsCompound = dto.IsCompound,
                        IsActive = true
                    }).ToList();

                    _context.BranchTaxes.AddRange(branchTaxes);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.Created;
                apiResponse.Message = "Tax created successfully.";
                apiResponse.Data = tax;
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


        // ================== UPDATE ==================

        public async Task<ApiResponse> UpdateAsync(UpdateTaxRequest dto)
        {
            var apiResponse = new ApiResponse();
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Validate logged-in user
                var userId = _userContext.GetUserId();
                if (!userId.HasValue)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    apiResponse.Message = "Unauthorized user.";
                    return apiResponse;
                }

                // Step 2: Role check (only SuperAdmin can update)
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId.Value);
                var roleCheck = roleCheckResponse?.Data as BaseRepository.RoleCheckResult;
                if (roleCheck == null || !roleCheck.IsSuperAdmin)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                    apiResponse.Message = "Only SuperAdmin can update taxes.";
                    return apiResponse;
                }

                // Step 3: Find tax
                var tax = await _context.Taxs
                    .FirstOrDefaultAsync(t => t.TaxId == dto.TaxId && (t.IsDeleted == null || t.IsDeleted == false));

                if (tax == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Tax not found.";
                    return apiResponse;
                }

                // Step 4: Validate HeadOfficeId (if changing)
                if (dto.HeadOfficeId.HasValue)
                {
                    var headOfficeExists = await _context.HeadOffices
                        .AnyAsync(h => h.HeadOfficeId == dto.HeadOfficeId.Value && !h.IsDeleted);
                    if (!headOfficeExists)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                        return apiResponse;
                    }

                    tax.HeadOfficeId = dto.HeadOfficeId.Value;
                }

                // Step 5: Prevent duplicate tax codes within the same HeadOffice
                var duplicateTax = await _context.Taxs.AnyAsync(t =>
                    t.Code == dto.Code &&
                    t.HeadOfficeId == tax.HeadOfficeId &&
                    t.TaxId != dto.TaxId &&
                    (t.IsDeleted == null || t.IsDeleted == false));

                if (duplicateTax)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "Another tax with the same code already exists in this HeadOffice.";
                    return apiResponse;
                }

                // Step 6: Update core tax fields
                tax.Code = dto.Code?.Trim();
                tax.Name = dto.Name?.Trim();
                tax.Rate = dto.Rate;
                tax.IsPercentage = dto.IsPercentage;
                tax.IsCompound = dto.IsCompound;
                tax.Priority = dto.Priority;
                tax.PaymentTypeId = dto.PaymentTypeId;
                tax.PaymentType = dto.PaymentType;
                tax.IsActive = dto.IsActive;
                tax.UpdatedAt = DateTime.UtcNow;
                tax.UpdatedBy = userId.Value;

                _context.Taxs.Update(tax);
                await _context.SaveChangesAsync();

                // Step 7: If BranchIds provided, update mappings
                if (dto.BranchIds != null)
                {
                    // Remove existing branch-tax mappings
                    var existingMappings = await _context.BranchTaxes
                        .Where(bt => bt.TaxId == tax.TaxId)
                        .ToListAsync();

                    if (existingMappings.Any())
                    {
                        _context.BranchTaxes.RemoveRange(existingMappings);
                        await _context.SaveChangesAsync();
                    }

                    if (dto.BranchIds.Any())
                    {
                        var requestedBranchIds = dto.BranchIds.Distinct().ToList();

                        // Validate branch ids
                        var validBranchIds = await _context.Branches
                            .Where(b => requestedBranchIds.Contains(b.BranchId)
                                        && !b.IsDeleted
                                        && (!dto.HeadOfficeId.HasValue || b.HeadOfficeId == dto.HeadOfficeId))
                            .Select(b => b.BranchId)
                            .ToListAsync();

                        var invalidBranchIds = requestedBranchIds.Except(validBranchIds).ToList();
                        if (invalidBranchIds.Any())
                        {
                            await transaction.RollbackAsync();
                            apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                            apiResponse.Message = $"Invalid or mismatched branchIds: {string.Join(',', invalidBranchIds)}";
                            return apiResponse;
                        }

                        // Add updated mappings
                        var branchTaxes = validBranchIds.Select(bid => new BranchTaxis
                        {
                            BranchId = bid,
                            TaxId = tax.TaxId,
                            Priority = dto.Priority,
                            IsCompound = dto.IsCompound,
                            IsActive = true
                        }).ToList();

                        _context.BranchTaxes.AddRange(branchTaxes);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Tax updated successfully.";
                apiResponse.Data = tax;
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


        // ================== DELETE ==================

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            try
            {
                var userId = _userContext.GetUserId().Value;
                var roleCheck = await GetRoleCheck(userId);
                if (roleCheck == null || (!roleCheck.IsSuperAdmin))
                    return ForbiddenResponse("Only SuperAdmin can delete taxes.");

                var tax = await _context.Taxs.FirstOrDefaultAsync(t => t.TaxId == id && !(t.IsDeleted.HasValue && t.IsDeleted.Value == true));
                if (tax == null)
                    return NotFoundResponse("Tax not found or already deleted.");

                tax.IsDeleted = true;
                tax.IsActive = false;
                tax.UpdatedAt = DateTime.UtcNow;
                tax.UpdatedBy = userId;

                _context.Taxs.Update(tax);
                await _context.SaveChangesAsync();

                return SuccessResponse("Tax deleted successfully.", null);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex);
            }
        }

        // ================== ASSIGN TO BRANCH ==================

        public async Task<ApiResponse> AssignTaxToBranchAsync(AssignBranchTaxRequest dto)
        {
            try
            {
                var userId = _userContext.GetUserId().Value;
                var roleCheck = await GetRoleCheck(userId);
                if (roleCheck == null || (!roleCheck.IsSystemAdmin && !roleCheck.IsSuperAdmin && !roleCheck.IsAdmin))
                    return ForbiddenResponse("User does not have permission to assign taxes to branches.");

                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == dto.BranchId && !b.IsDeleted);
                if (branch == null)
                    return NotFoundResponse("Branch not found.");

                var tax = await _context.Taxs.FirstOrDefaultAsync(t => t.TaxId == dto.TaxId && !(t.IsDeleted.HasValue && t.IsDeleted.Value == true));
                if (tax == null)
                    return NotFoundResponse("Tax not found.");

                var branchTax = new BranchTaxis
                {
                    BranchId = dto.BranchId,
                    TaxId = dto.TaxId,
                    Priority = dto.Priority,
                    IsCompound = dto.IsCompound,
                    IsActive = dto.IsActive ?? true
                };

                _context.BranchTaxes.Add(branchTax);
                await _context.SaveChangesAsync();

                return CreatedResponse("Tax assigned to branch successfully.", branchTax);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex);
            }
        }

        // ================== GET BRANCH TAXES ==================

        public async Task<ApiResponse> GetBranchTaxesAsync(int branchId)
        {
            try
            {
                var userId = _userContext.GetUserId().Value;
                var roleCheck = await GetRoleCheck(userId);
                if (roleCheck == null)
                    return UnauthorizedResponse("Unauthorized User");

                IQueryable<BranchTaxis> query = _context.BranchTaxes
                    .Where(bt => bt.BranchId == branchId && (bt.IsActive == null || bt.IsActive == true));

                if (roleCheck.IsSystemAdmin)
                {
                    // no restriction
                }
                else if (roleCheck.IsSuperAdmin)
                {
                    var headOfficeId = await _context.Users
                        .Where(u => u.UserId == userId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    var branchIds = await _context.Branches
                        .Where(b => b.HeadOfficeId == headOfficeId && !b.IsDeleted)
                        .Select(b => b.BranchId)
                        .ToListAsync();

                    if (!branchIds.Contains(branchId))
                        return ForbiddenResponse("SuperAdmin can only access their HeadOffice branches.");
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {
                    var branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == userId && (ub.IsActive == null || ub.IsActive == true) && (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();

                    if (!branchIds.Contains(branchId))
                        return ForbiddenResponse("User cannot access taxes of this branch.");
                }
                else
                {
                    return ForbiddenResponse("User does not have permission to view Branch taxes.");
                }

                var taxes = await query.AsNoTracking().ToListAsync();
                return SuccessResponse("Branch taxes retrieved successfully.", taxes);
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex);
            }
        }

        // ================== HELPERS ==================

        private async Task<BaseRepository.RoleCheckResult?> GetRoleCheck(int userId)
        {
            var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
            return roleCheckResponse.Data as BaseRepository.RoleCheckResult;
        }

        private ApiResponse SuccessResponse(string msg, object? data) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.OK, Message = msg, Data = data };

        private ApiResponse CreatedResponse(string msg, object? data) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.Created, Message = msg, Data = data };

        private ApiResponse NotFoundResponse(string msg) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.NotFound, Message = msg, Data = null };

        private ApiResponse ForbiddenResponse(string msg) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.Forbidden, Message = msg, Data = null };

        private ApiResponse UnauthorizedResponse(string msg) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.Unauthorized, Message = msg, Data = null };

        private ApiResponse ErrorResponse(Exception ex) =>
            new ApiResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Message = ex.InnerException?.Message ?? ex.Message, Data = null };
    }

}
