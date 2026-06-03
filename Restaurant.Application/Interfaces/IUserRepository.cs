using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{

    public interface IUserRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddAsync(UserDto dto);
        Task<ApiResponse> UpdateAsync(UserDto dto);
        Task<ApiResponse> DeleteAsync(int Id);
        Task<ApiResponse> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<User?> GetByUsernameAsync(string username);

    }
}
