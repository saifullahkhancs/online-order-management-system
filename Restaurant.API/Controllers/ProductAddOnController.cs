using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.Net;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")]
    public class ProductAddOnController : ControllerBase
    {
        private readonly IProductAddOnRepository _repository;
        private readonly Cloudinary _cloudinary;
        
        public ProductAddOnController(IProductAddOnRepository repository, Cloudinary cloudinary)
        {
            _repository = repository;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(int HeadOfficeId)
        {
            try
            {
                var response = await _repository.GetAllAsync(HeadOfficeId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductAddOn?>> GetById(int id)
        {
            try
            {
                var response = await _repository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("product/{productId:int}")]
        public async Task<ActionResult> GetByProductId(int productId)
        {
            try
            {
                var response = await _repository.GetByProductIdAsync(productId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("addon/{addOnId:int}")]
        public async Task<ActionResult> GetByAddOnId(int addOnId)
        {
            try
            {
                var response = await _repository.GetByAddOnIdAsync(addOnId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("AddMultiple")]
        public async Task<ActionResult> AddMultiple([FromBody] List<ProductAddOnVM> entities)
        {
            try
            {
                var productAddOns = entities.Select(entity => new ProductAddOn
                {
                    ProductId = entity.ProductId,
                    AddOnId = entity.AddOnId,
                    HeadOfficeId = entity.HeadOfficeId
                }).ToList();

                var response = await _repository.AddMultipleAsync(productAddOns);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ProductAddOnVM entity)
        {
            try
            {
                var productAddOn = new ProductAddOn
                {
                    ProductId = entity.ProductId,
                    AddOnId = entity.AddOnId,
                    HeadOfficeId = entity.HeadOfficeId
                };
                
                var response = await _repository.AddAsync(productAddOn);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] ProductAddOnVM entity)
        {
            try
            {
                if (id != entity.Id)
                {
                    return BadRequest("Product add-on ID mismatch");
                }

                var productAddOn = new ProductAddOn
                {
                    Id = entity.Id,
                    ProductId = entity.ProductId,
                    AddOnId = entity.AddOnId,
                    HeadOfficeId = entity.HeadOfficeId
                };

                var response = await _repository.UpdateAsync(productAddOn);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
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
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}