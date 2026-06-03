using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Restaurant.API.Hubs;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using System.Net;
using System.Security.Claims;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderController(IOrderRepository orderRepository, IHubContext<OrderHub> hubContext)
        {
            _orderRepository = orderRepository;
            _hubContext = hubContext;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDto dto)
        {
            try
            {
                var response = await _orderRepository.PlaceOrderAsync(dto);
                // ClientPrintAgent Work on hold
                //if (response != null && response.StatusCode == 201) 
                //{
                //    var orderDtO = response.Data as OrderDto;
                //    // Broadcast to connected clients (branch admins)
                //    await _hubContext.Clients
                //        .Group($"branch-{orderDtO.BranchId}")
                //        .SendAsync("ReceiveOrder", orderDtO);
                //}

                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("cancel/{orderId:int}")]
        public async Task<IActionResult> CancelOrder(long orderId,[FromBody] CancelOrderRequest? request)
        {
            var response = await _orderRepository.CancelOrderAsync(orderId, request?.GuestSessionToken, request?.CancelReason);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("order-status/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(long orderId)
        {
            try
            {
                var response = await _orderRepository.GetOrderStatusAsync(orderId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(long orderId, long customerId, [FromQuery] Guid? guestToken = null)
        {
            try
            {
                var result = await _orderRepository.GetOrderByIdAsync(orderId, guestToken, customerId);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        #region ----------------------- Manage Orders API ----------------------------

        [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin,OrderTaker")] // Only privileged roles can manage roles
        [HttpGet("GetLiveOrders")]
        public async Task<ActionResult<ApiResponse>> GetLiveOrders()
        {
            try
            {
                var response = await _orderRepository.GetLiveOrdersAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
        }

        [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin,OrderTaker")] // Only privileged roles can manage roles
        [HttpGet("GetAllOrders")]
        public async Task<ActionResult<ApiResponse>> GetAllOrders()
        {
            try
            {
                var response = await _orderRepository.GetAllOrdersAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
        }

        [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin,OrderTaker")] // Only privileged roles can manage roles
        [HttpPut("update-status")]
        public async Task<ActionResult<ApiResponse>> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest dto)
        {
            try
            {
                var response = await _orderRepository.UpdateOrderStatusAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
           
        }

        [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin,OrderTaker")] // Only privileged roles can manage roles
        [HttpGet("available-statuses/{orderId}")]
        public async Task<ActionResult<List<AvailableStatusOption>>> GetAvailableStatuses(int orderId)
        {
            try
            {
                var response = await _orderRepository.GetAvailableStatusesAsync(orderId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
        }

        [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin,OrderTaker")] // Only privileged roles can manage roles
        [HttpGet("GetAllOrderStatuses")]
        public async Task<ActionResult<List<AvailableStatusOption>>> GetAllOrderStatuses()
        {
            try
            {
                var response = await _orderRepository.GetAllOrderStatuses(null);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
        }

        #endregion

    }
}
