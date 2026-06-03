using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.Net;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage Branch Products
    public class BranchProductController : ControllerBase
    {
        private readonly IBranchProductRepository _repository;

        public BranchProductController(IBranchProductRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(int branchId)
        {
            try
            {
                var response = await _repository.GetAllAsync(branchId);
                return StatusCode(response.StatusCode,response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpGet("{branchproductid:int}")]
        public async Task<ActionResult<BranchProduct?>> GetById(int branchproductid)
        {
            try
            {
                var response = await _repository.GetByIdAsync(branchproductid);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }
        // add multiple branch product

        [HttpPost("AddMultiple")]
        public async Task<ActionResult> AddMultiple([FromBody] List<BranchProductVM> entities)
        {
            try
            {
                var branchProducts = entities.Select(entity => new BranchProduct
                {
                    BranchId = entity.BranchId,
                    ProductId = entity.ProductId,
                    Price = entity.Price,
                    IsActive = entity.IsActive
                }).ToList();

                var response = await _repository.AddMultipleAsync(branchProducts);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.ToString() ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BranchProductVM entity)
        {
            try
            {
                var branchProduct = new BranchProduct
                {
                    BranchId = entity.BranchId,
                    ProductId = entity.ProductId,
                    Price = entity.Price,
                    IsActive = entity.IsActive
                };
                var response = await _repository.AddAsync(branchProduct);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] BranchProductVM entity)
        {
            try
            {
                if (id != entity.BranchProductId)
                {
                    return BadRequest("ID mismatch");
                }
                var branchProduct = new BranchProduct
                {
                    BranchProductId = entity.BranchProductId,
                    BranchId = entity.BranchId,
                    ProductId = entity.ProductId,
                    Price = entity.Price,
                    IsActive = entity.IsActive
                };
                var response = await _repository.UpdateAsync(branchProduct);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var response = await _repository.DeleteAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }
    }
}