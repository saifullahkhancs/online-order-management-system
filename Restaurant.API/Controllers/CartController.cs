using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;

        public CartController(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetById(int? userId = null, Guid? GuestSessionToken = null)
        {
            try
            {
                var response = await _cartRepository.GetByIdAsync(userId, GuestSessionToken);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("AddItem")]
        public async Task<ActionResult<ApiResponse>> AddItem([FromBody] AddCartItemRequest dto)
        {
            try
            {
                var response = await _cartRepository.AddItemAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        //[HttpPut("UpdateItem")]
        //public async Task<ActionResult<ApiResponse>> UpdateItem([FromBody] UpdateCartItemRequest dto)
        //{
        //    try
        //    {
        //        var response = await _cartRepository.UpdateItemAsync(dto);
        //        return StatusCode(response.StatusCode, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
        //    }
        //}

        [HttpDelete("RemoveItem/{cartItemId}")]
        public async Task<ActionResult<ApiResponse>> RemoveItem(int cartItemId)
        {
            try
            {
                var response = await _cartRepository.RemoveItemAsync(cartItemId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("Clear/{cartId}")]
        public async Task<ActionResult<ApiResponse>> ClearCart(int cartId)
        {
            try
            {
                var response = await _cartRepository.ClearCartAsync(cartId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
