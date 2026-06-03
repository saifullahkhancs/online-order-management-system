using Restaurant.Domain.Entities;
using Restaurant.Application.Dtos;

namespace Restaurant.Application.Interfaces
{
    public interface IModifierRepository
    {
        Task<ApiResponse> GetAllAsync(int headOfficeId);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddMultipleAsync(List<Modifier> modifiers);
        Task<ApiResponse> AddAsync(Modifier entity);
        Task<ApiResponse> UpdateAsync(Modifier entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}