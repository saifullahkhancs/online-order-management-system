using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IBranchProductRepository
    {
        Task<ApiResponse> GetAllAsync(int branchId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<BranchProduct> branchProducts);
        Task<ApiResponse> AddAsync(BranchProduct entity);
        Task<ApiResponse> UpdateAsync(BranchProduct entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}