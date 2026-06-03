using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
namespace Restaurant.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddAsync(Category entity);
        Task<ApiResponse> UpdateAsync(Category entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}

