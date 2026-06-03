using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{

    public interface ICartRepository
    {
        Task<ApiResponse> GetByIdAsync(int? userId, Guid? GuestSessionToken);
        Task<ApiResponse> AddItemAsync(AddCartItemRequest dto);
        //Task<ApiResponse> UpdateItemAsync(UpdateCartItemRequest dto);
        Task<ApiResponse> RemoveItemAsync(int cartItemId);
        Task<ApiResponse> ClearCartAsync(int cartId);
    }

}
