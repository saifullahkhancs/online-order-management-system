using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContext;
        private readonly IBaseRepository _baseRepository;

        public UserRepository(AppDbContext context, UserContextService userContext,IBaseRepository baseRepository)
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
                    apiResponse.Data = new List<UserDto>();
                    return apiResponse;
                }

                // Step 2: Base query
                var query = from u in _context.Users
                            join h in _context.HeadOffices on u.HeadOfficeId equals h.HeadOfficeId into ho
                            from h in ho.DefaultIfEmpty()
                            where u.IsDeleted == false && u.Username != "systemadmin"
                            select new UserDto
                            {
                                UserId = u.UserId,
                                Username = u.Username,
                                Email = u.Email,
                                PhoneNumber = u.PhoneNumber,
                                HeadOfficeId = u.HeadOfficeId,
                                HeadOfficeName = h.Name,
                                IsActive = u.IsActive,
                                IsDeleted = u.IsDeleted,

                                Branches = (
                                    from ub in _context.UserBranches
                                    join b in _context.Branches on ub.BranchId equals b.BranchId
                                    where ub.UserId == u.UserId && (ub.IsDeleted == false || ub.IsDeleted == null)
                                    select new UserBranchDto
                                    {
                                        BranchId = b.BranchId,
                                        BranchName = b.BranchName
                                    }
                                ).ToList(),

                                Roles = (
                                    from ur in _context.UserRoles
                                    join r in _context.Roles on ur.RoleId equals r.RoleId
                                    where ur.UserId == u.UserId && ur.IsActive == true
                                    select new UserRoleDto
                                    {
                                        RoleId = r.RoleId,
                                        RoleName = r.RoleName
                                    }
                                ).ToList()
                            };

                // Step 3: Apply role-based restrictions
                if (roleCheck.IsSystemAdmin)
                {
                    // SystemAdmin → sees all users
                }
                else if (roleCheck.IsSuperAdmin)
                {
                    // SuperAdmin → only users under their HeadOffice
                    var userHeadOfficeId = await _context.Users
                        .Where(u => u.UserId == userId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    query = query.Where(u => u.HeadOfficeId == userHeadOfficeId);
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {
                    // Admin → only users in their assigned branches
                    var branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == userId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();

                    query = query.Where(u =>
                        (from ub in _context.UserBranches
                         where ub.UserId == u.UserId
                         select ub.BranchId).Any(bid => branchIds.Contains(bid)));
                }
                //else if (roleCheck.IsOrderTaker)
                //{
                //    // OrderTaker → only sees himself
                //    query = query.Where(u => u.UserId == userId);
                //}
                else
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "User does not have permission to view Users.";
                    apiResponse.Data = new List<UserDto>();
                    return apiResponse;
                }

                // Step 4: Execute
                var users = await query.AsNoTracking().ToListAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Users retrieved successfully!";
                apiResponse.Data = users;
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
                var loggedInUserId = _userContext.GetUserId().Value;

                // Step 1: Check roles of the logged-in user
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(loggedInUserId);
                var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

                if (roleCheck == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "Unauthorized User";
                    apiResponse.Data = null;
                    return apiResponse;
                }

                // Step 2: Build query
                var query = from u in _context.Users
                            where u.UserId == id && u.IsDeleted == false
                            select new UserDto
                            {
                                UserId = u.UserId,
                                Username = u.Username,
                                Email = u.Email,
                                PhoneNumber = u.PhoneNumber,
                                HeadOfficeId = u.HeadOfficeId,
                                IsActive = u.IsActive,
                                IsDeleted = u.IsDeleted,

                                Branches = (
                                    from ub in _context.UserBranches
                                    join b in _context.Branches on ub.BranchId equals b.BranchId
                                    where ub.UserId == u.UserId && (ub.IsDeleted == false || ub.IsDeleted == null)
                                    select new UserBranchDto
                                    {
                                        BranchId = b.BranchId,
                                        BranchName = b.BranchName
                                    }
                                ).ToList(),

                                Roles = (
                                    from ur in _context.UserRoles
                                    join r in _context.Roles on ur.RoleId equals r.RoleId
                                    where ur.UserId == u.UserId && ur.IsActive == true
                                    select new UserRoleDto
                                    {
                                        RoleId = r.RoleId,
                                        RoleName = r.RoleName
                                    }
                                ).ToList()
                            };

                // Step 3: Apply role-based restrictions
                if (roleCheck.IsSystemAdmin)
                {
                    // Full access
                }
                else if (roleCheck.IsSuperAdmin)
                {
                    // Can only view users under their HeadOffice
                    var userHeadOfficeId = await _context.Users
                        .Where(u => u.UserId == loggedInUserId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    query = query.Where(u => u.HeadOfficeId == userHeadOfficeId);
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {
                    // Can only view users in their assigned branches
                    var branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == loggedInUserId && ub.IsActive == true && (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();

                    query = query.Where(u =>
                        (from ub in _context.UserBranches
                         where ub.UserId == u.UserId
                         select ub.BranchId).Any(bid => branchIds.Contains(bid)));
                }
                //else if (roleCheck.IsOrderTaker)
                //{
                //    // Can only view himself
                //    query = query.Where(u => u.UserId == loggedInUserId);
                //}
                else
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "User does not have permission to view this record.";
                    apiResponse.Data = null;
                    return apiResponse;
                }

                // Step 4: Execute
                var user = await query.AsNoTracking().FirstOrDefaultAsync();

                if (user == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "User not found or access denied.";
                    apiResponse.Data = null;
                    return apiResponse;
                }

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User retrieved successfully!";
                apiResponse.Data = user;
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


        public async Task<ApiResponse> AddAsync(UserDto dto)
        {
            var apiResponse = new ApiResponse();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //  Validate HeadOffice
                if (dto.HeadOfficeId.HasValue)
                {
                    var headOfficeExists = await _context.HeadOffices
                        .AnyAsync(h => h.HeadOfficeId == dto.HeadOfficeId && !h.IsDeleted);

                    if (!headOfficeExists)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                        return apiResponse;
                    }
                }

                //  Validate uniqueness
                var uniquenessCheck = await ValidateUserUniquenessAsync(dto.Email, dto.PhoneNumber);
                if (uniquenessCheck != null)
                    return uniquenessCheck;

                // Get Hash and Salt against Password
                var (hash, salt) = ComputeHashAndSalt(dto.Password);
                //  Map to DB entity
                var userId = _userContext.GetUserId();
                var dbEntity = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordSalt = salt,
                    PasswordHash = hash,
                    IsActive = dto.IsActive ?? true,
                    IsDeleted = false,
                    HeadOfficeId = dto.HeadOfficeId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.Users.Add(dbEntity);
                await _context.SaveChangesAsync();

                // update dto with generated ID
                dto.UserId = dbEntity.UserId;

                //  Assign Branches
                if (dto.Branches.Any())
                {
                    // validate branch IDs
                    var branchIds = dto.Branches.Select(b => b.BranchId).ToList();
                    var existingBranchIds = await _context.Branches
                        .Where(b => branchIds.Contains(b.BranchId) && !b.IsDeleted)
                        .Select(b => b.BranchId)
                        .ToListAsync();

                    if (branchIds.Except(existingBranchIds).Any())
                    {
                        await transaction.RollbackAsync();
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "One or more branches are invalid.";
                        return apiResponse;
                    }

                    var userBranches = dto.Branches.Select(b => new UserBranch
                    {
                        UserId = dbEntity.UserId,
                        BranchId = b.BranchId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        IsDeleted = false
                    }).ToList();

                    _context.UserBranches.AddRange(userBranches);
                }

                //  Assign Roles
                if (dto.Roles.Any())
                {
                    var roleIds = dto.Roles.Select(r => r.RoleId).ToList();
                    var existingRoleIds = await _context.Roles
                        .Where(r => roleIds.Contains(r.RoleId))
                        .Select(r => r.RoleId)
                        .ToListAsync();

                    if (roleIds.Except(existingRoleIds).Any())
                    {
                        await transaction.RollbackAsync();
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "One or more roles are invalid.";
                        return apiResponse;
                    }

                    var userRoles = dto.Roles.Select(r => new UserRole
                    {
                        UserId = dbEntity.UserId,
                        RoleId = r.RoleId,
                        IsActive = true
                    }).ToList();

                    _context.UserRoles.AddRange(userRoles);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.Created;
                apiResponse.Message = "User created successfully with roles and branches.";
                apiResponse.Data = dto;
                return apiResponse;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }

        public async Task<Domain.Entities.User?> GetByUsernameAsync(string email)
        {
            var dbUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive == true && ( !u.IsDeleted.HasValue || !u.IsDeleted!.Value));

            if (dbUser == null) return null;

            // Load roles
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == dbUser.UserId && ur.IsActive == true)
                .Join(
                    _context.Roles,
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => new Domain.Entities.Role
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName!,
                        RoleDescription = r.RoleDescription,
                        IsActive = r.IsActive ?? false
                    }
                ).ToListAsync();

            // Load branches
            var userBranches = await _context.UserBranches
                .Where(ub => ub.UserId == dbUser.UserId && ub.IsDeleted == false)
                .Join(
                    _context.Branches,
                    ub => ub.BranchId,
                    b => b.BranchId,
                    (ub, b) => new Domain.Entities.Branch
                    {
                        BranchId = b.BranchId,
                        BranchName = b.BranchName
                    }
                ).ToListAsync();

            // Load head office
            var headOffice = await _context.HeadOffices
                .Where(h => h.HeadOfficeId == dbUser.HeadOfficeId && !h.IsDeleted)
                .Select(h => new Domain.Entities.HeadOffice
                {
                    HeadOfficeId = h.HeadOfficeId,
                    Name = h.Name
                })
                .FirstOrDefaultAsync();

            return new Domain.Entities.User
            {
                UserId = dbUser.UserId,
                Username = dbUser.Username,
                Email = dbUser.Email,
                PasswordHash = dbUser.PasswordHash,
                PasswordSalt = dbUser.PasswordSalt,
                HeadOfficeId = dbUser.HeadOfficeId,
                HeadOffice = headOffice,
                Branches = userBranches,
                Roles = userRoles
            };
        }


        public async Task<ApiResponse> UpdateAsync(UserDto dto)
        {
            var apiResponse = new ApiResponse();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dbEntity = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == dto.UserId && (!u.IsDeleted.HasValue! || !u.IsDeleted.Value));

                if (dbEntity == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "User not found.";
                    return apiResponse;
                }

                // Validate HeadOffice
                if (dto.HeadOfficeId.HasValue)
                {
                    var headOfficeExists = await _context.HeadOffices
                        .AnyAsync(h => h.HeadOfficeId == dto.HeadOfficeId && !h.IsDeleted);

                    if (!headOfficeExists)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "Invalid HeadOfficeId. Head office does not exist.";
                        return apiResponse;
                    }
                }

                //  Validate uniqueness (exclude current user)
                var uniquenessCheck = await ValidateUserUniquenessAsync(dto.Email, dto.PhoneNumber, dto.UserId);
                if (uniquenessCheck != null)
                    return uniquenessCheck;

                var userId = _userContext.GetUserId();

                //  Update base user properties
                dbEntity.Username = dto.Username;
                dbEntity.Email = dto.Email;
                dbEntity.PhoneNumber = dto.PhoneNumber;
                dbEntity.IsActive = dto.IsActive ?? dbEntity.IsActive;
                dbEntity.HeadOfficeId = dto.HeadOfficeId;
                dbEntity.UpdatedAt = DateTime.UtcNow;
                dbEntity.UpdatedBy = userId;

                _context.Users.Update(dbEntity);
                await _context.SaveChangesAsync();

                //  Update Branches
                var existingUserBranches = await _context.UserBranches
                    .Where(ub => ub.UserId == dbEntity.UserId)
                    .ToListAsync();

                _context.UserBranches.RemoveRange(existingUserBranches);

                if (dto.Branches.Any())
                {
                    var branchIds = dto.Branches.Select(b => b.BranchId).ToList();
                    var existingBranchIds = await _context.Branches
                        .Where(b => branchIds.Contains(b.BranchId) && !b.IsDeleted)
                        .Select(b => b.BranchId)
                        .ToListAsync();

                    if (branchIds.Except(existingBranchIds).Any())
                    {
                        await transaction.RollbackAsync();
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "One or more branches are invalid.";
                        return apiResponse;
                    }

                    var userBranches = dto.Branches.Select(b => new UserBranch
                    {
                        UserId = dbEntity.UserId,
                        BranchId = b.BranchId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        IsDeleted = false
                    }).ToList();

                    _context.UserBranches.AddRange(userBranches);
                }

                //  Update Roles
                var existingUserRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == dbEntity.UserId)
                    .ToListAsync();

                _context.UserRoles.RemoveRange(existingUserRoles);

                if (dto.Roles.Any())
                {
                    var roleIds = dto.Roles.Select(r => r.RoleId).ToList();
                    var existingRoleIds = await _context.Roles
                        .Where(r => roleIds.Contains(r.RoleId))
                        .Select(r => r.RoleId)
                        .ToListAsync();

                    if (roleIds.Except(existingRoleIds).Any())
                    {
                        await transaction.RollbackAsync();
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "One or more roles are invalid.";
                        return apiResponse;
                    }

                    var userRoles = dto.Roles.Select(r => new UserRole
                    {
                        UserId = dbEntity.UserId,
                        RoleId = r.RoleId,
                        IsActive = true
                    }).ToList();

                    _context.UserRoles.AddRange(userRoles);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User updated successfully with roles and branches.";
                apiResponse.Data = dto;
                return apiResponse;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }


        public async Task<ApiResponse> DeleteAsync(int Id)
        {
            var apiResponse = new ApiResponse();

            try
            {
                var dbEntity = await _context.Users
                    .FirstOrDefaultAsync(x => x.UserId == Id 
                        && (!x.IsDeleted.HasValue! || !x.IsDeleted.Value));


                if (dbEntity == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "User not found or already deleted.";
                    apiResponse.Data = null;
                    return apiResponse;
                }

                var userId = _userContext.GetUserId();

                // Soft delete
                dbEntity.IsDeleted = true;
                dbEntity.IsActive = false;
                dbEntity.UpdatedAt = DateTime.UtcNow;
                dbEntity.UpdatedBy = userId;

                _context.Users.Update(dbEntity);
                await _context.SaveChangesAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "User deleted successfully.";
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

        public async Task<ApiResponse> ResetPassword(ResetPasswordDto dto)
        {
            var apiResponse = new ApiResponse();
            try
            {
                // Find user by UserId + Email/Phone (double check to avoid accidental reset)
                var dbUser = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == dto.UserId &&
                        (u.Email == dto.Email || u.PhoneNumber == dto.PhoneNumber) &&
                        ( !u.IsDeleted.HasValue || !u.IsDeleted.Value));

                if (dbUser == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "User not found with provided details.";
                    return apiResponse;
                }

                // Compute new hash + salt
                var (hash, salt) = ComputeHashAndSalt(dto.NewPassword);

                dbUser.PasswordHash = hash;
                dbUser.PasswordSalt = salt;
                dbUser.UpdatedAt = DateTime.UtcNow;
                dbUser.UpdatedBy = _userContext.GetUserId();

                _context.Users.Update(dbUser);
                await _context.SaveChangesAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Password reset successfully.";
                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }

        // Helper Private Methods
        private async Task<ApiResponse?> ValidateUserUniquenessAsync(string? email, string? phoneNumber, int? userId = null)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == email && (!userId.HasValue || u.UserId != userId.Value));

                if (emailExists)
                {
                    return new ApiResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Email address already exists."
                    };
                }
            }

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == phoneNumber && (!userId.HasValue || u.UserId != userId.Value));

                if (phoneExists)
                {
                    return new ApiResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Phone number already exists."
                    };
                }
            }

            return null; // validation passed
        }


        private (byte[] Hash, byte[] Salt) ComputeHashAndSalt(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            // Generate salt and hash
            using var hmac = new HMACSHA512();
            var salt = hmac.Key; // 512-bit random key
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return (hash, salt);
        }

    }
}
