using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.Net;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage Products
    public class MenusController : ControllerBase
    {
        private readonly IMenuRepository _repository;

        public MenusController(IMenuRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]

        // get menu based on branch id
        // api take branch id as param and return menu for that branch
        public async Task<ActionResult> GetMenu(int branchId)
        {
            try
            {
                var response = await _repository.GetMenu(branchId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("GetBranchByRestId/{restId}")]
        public async Task<ActionResult> GetAllRestaurantBranchesAsync(int restId)
        {
            try
            {
                var response = await _repository.GetAllRestaurantBranchesAsync(restId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("GetProductDetails")]
        public async Task<ActionResult> GetProductDetails(int productId)
        {
            try
            {
                var response = await _repository.GetProductDetails(productId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }


    }
}