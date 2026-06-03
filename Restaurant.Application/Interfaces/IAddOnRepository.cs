using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IAddOnRepository
    {
        Task<ApiResponse> GetAllAsync(int headOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<AddOn> branchProducts);
        Task<ApiResponse> AddAsync(AddOn entity);
        Task<ApiResponse> UpdateAsync(AddOn entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}