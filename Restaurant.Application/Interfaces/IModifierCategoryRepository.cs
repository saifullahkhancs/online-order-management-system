using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IModifierCategoryRepository
    {
        Task<ApiResponse> GetAllAsync(int headOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<ModifierCategory> modifiers);
        Task<ApiResponse> AddAsync(ModifierCategory entity);
        Task<ApiResponse> UpdateAsync(ModifierCategory entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}