using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{

    public interface IOrderRepository
    {
        Task<ApiResponse> PlaceOrderAsync(OrderDto dto);
        Task<ApiResponse> GetOrderStatusAsync(long orderId);
        Task<ApiResponse> GetOrderByIdAsync(long orderId, Guid? guestSessionToken = null, long? customerId = null);
        Task<ApiResponse> CancelOrderAsync(long orderId, Guid? guestSessionToken = null, string? cancelReason = null);

        #region ----------------------- Manage Orders API ----------------------------
        Task<ApiResponse> GetLiveOrdersAsync();
        Task<ApiResponse> GetAllOrdersAsync();
        Task<ApiResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request);
        Task<ApiResponse> GetAvailableStatusesAsync(int orderId);
        Task<ApiResponse> GetAllOrderStatuses(int? headOfficeId);
        #endregion
    }

}
