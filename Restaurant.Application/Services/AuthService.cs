using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<ApiResponse> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(email);
            if (user == null)
            {
                return new ApiResponse
                {
                    StatusCode = 401,
                    Message = "Invalid email or password"
                };
            }

            // Verify password
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return new ApiResponse
                {
                    StatusCode = 401,
                    Message = "Invalid email or password"
                };
            }

            // Generate JWT
            var token = await GenerateJwtToken(user);

            // Full user response
            var userDetails = new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.HeadOfficeId,
                HeadOfficeName = user.HeadOffice?.Name,
                Branches = user.Branches?.Select(b => new UserBranchDto { BranchId = b.BranchId, BranchName=b.BranchName }),
                Roles = user.Roles?.Select(r => new UserRoleDto { RoleId = r.RoleId, RoleName = r.RoleName }),
                Token = token
            };

            return new ApiResponse
            {
                StatusCode = 200,
                Message = "Login successful",
                Data = userDetails
            };
        }


        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return storedHash.SequenceEqual(computedHash);
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim("username", user.Username ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            // Add HeadOfficeId if exists
            if (user.HeadOfficeId.HasValue)
            {
                claims.Add(new Claim("headOfficeId", user.HeadOfficeId.Value.ToString()));
            }

            // Add Roles
            if (user.Roles != null && user.Roles.Any())
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim("role", role.RoleName));
                }
            }

            // Add BranchIds only (lightweight)
            if (user.Branches != null && user.Branches.Any())
            {
                foreach (var branch in user.Branches)
                {
                    claims.Add(new Claim("branchId", branch.BranchId.ToString()));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
