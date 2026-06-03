
using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces
{
    public interface IHeadOfficeRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> AddAsync(HeadOfficeDto entity);
        Task<ApiResponse> UpdateAsync(HeadOfficeDto entity);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
