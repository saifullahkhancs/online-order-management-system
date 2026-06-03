
using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IBranchRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddAsync(Branch entity);
        Task<ApiResponse> UpdateAsync(Branch entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
