using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IProductAddOnRepository
    {
        Task<ApiResponse> GetAllAsync(int HeadOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<ProductAddOn> productAddOns);
        Task<ApiResponse> AddAsync(ProductAddOn entity);
        Task<ApiResponse> UpdateAsync(ProductAddOn entity);
        Task<ApiResponse> DeleteAsync(int id);
        Task<ApiResponse> GetByProductIdAsync(int productId);
        Task<ApiResponse> GetByAddOnIdAsync(int addOnId);
    }
}