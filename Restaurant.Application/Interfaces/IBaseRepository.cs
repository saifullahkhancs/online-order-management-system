
using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IBaseRepository
    {
        Task<ApiResponse> GetRoleCheckResultAsync(int userId);
        
    }
}
