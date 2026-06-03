using Restaurant.API.Models;
using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IAddOnCategoryRepository
    {
        Task<ApiResponse> GetAllAsync(int headOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<AddOnCategoryDto> branchProducts);
        Task<ApiResponse> AddAsync(AddOnCategoryDto entity);
        Task<ApiResponse> UpdateAsync(AddOnCategoryDto entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}