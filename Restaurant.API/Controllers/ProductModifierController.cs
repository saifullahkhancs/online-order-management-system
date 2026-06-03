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
    public class ProductModifierController : ControllerBase
    {
        private readonly IProductModifierRepository _repository;
        private readonly Cloudinary _cloudinary;
        
        public ProductModifierController(IProductModifierRepository repository, Cloudinary cloudinary)
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
        public async Task<ActionResult<ProductModifier?>> GetById(int id)
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

        [HttpGet("modifier/{modifierId:int}")]
        public async Task<ActionResult> GetByModifierId(int modifierId)
        {
            try
            {
                var response = await _repository.GetByModifierIdAsync(modifierId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("AddMultiple")]
        public async Task<ActionResult> AddMultiple([FromBody] List<ProductModifierVM> entities)
        {
            try
            {
                var productModifiers = entities.Select(entity => new ProductModifier
                {
                    ProductId = entity.ProductId,
                    ModifierId = entity.ModifierId,
                    HeadOfficeId = entity.HeadOfficeId
                }).ToList();

                var response = await _repository.AddMultipleAsync(productModifiers);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ProductModifierVM entity)
        {
            try
            {
                var productModifier = new ProductModifier
                {
                    ProductId = entity.ProductId,
                    ModifierId = entity.ModifierId,
                    HeadOfficeId = entity.HeadOfficeId
                };
                
                var response = await _repository.AddAsync(productModifier);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] ProductModifierVM entity)
        {
            try
            {
                if (id != entity.Id)
                {
                    return BadRequest("Product modifier ID mismatch");
                }

                var productModifier = new ProductModifier
                {
                    Id = entity.Id,
                    ProductId = entity.ProductId,
                    ModifierId = entity.ModifierId,
                    HeadOfficeId = entity.HeadOfficeId
                };

                var response = await _repository.UpdateAsync(productModifier);
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