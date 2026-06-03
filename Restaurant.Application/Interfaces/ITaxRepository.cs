using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{

    public interface ITaxRepository
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> CreateAsync(CreateTaxRequest dto);
        Task<ApiResponse> UpdateAsync(UpdateTaxRequest dto);
        Task<ApiResponse> DeleteAsync(int id);

        Task<ApiResponse> AssignTaxToBranchAsync(AssignBranchTaxRequest dto);
        Task<ApiResponse> GetBranchTaxesAsync(int branchId);
    }
}
