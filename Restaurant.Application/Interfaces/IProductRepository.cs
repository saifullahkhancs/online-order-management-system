using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddAsync(Product entity);
        Task<ApiResponse> UpdateAsync(Product entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
