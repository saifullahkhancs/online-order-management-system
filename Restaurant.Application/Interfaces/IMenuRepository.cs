using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IMenuRepository
    {
        Task<ApiResponse> GetMenu(int branchId);

        Task<ApiResponse> GetAllRestaurantBranchesAsync(int restaurantId);

        Task<ApiResponse> GetProductDetails(int productId);   
    }
        
}