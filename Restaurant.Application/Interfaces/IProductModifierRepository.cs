using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IProductModifierRepository
    {
        Task<ApiResponse> GetAllAsync(int HeadOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<ProductModifier> productModifiers);
        Task<ApiResponse> AddAsync(ProductModifier entity);
        Task<ApiResponse> UpdateAsync(ProductModifier entity);
        Task<ApiResponse> DeleteAsync(int id);
        Task<ApiResponse> GetByProductIdAsync(int productId);
        Task<ApiResponse> GetByModifierIdAsync(int modifierId);
    }
}