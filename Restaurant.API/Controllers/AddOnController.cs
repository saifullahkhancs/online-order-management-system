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
    public class AddOnController : ControllerBase
    {
        private readonly IAddOnRepository _repository;
        
        public AddOnController(IAddOnRepository repository, Cloudinary cloudinary)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(int headOfficeId)
        {
            try
            {
                var response = await _repository.GetAllAsync(headOfficeId);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AddOn?>> GetById(int id)
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

        [HttpPost("AddMultiple")]
        public async Task<ActionResult> AddMultiple([FromBody] List<AddOnVM> entities)
        {
            try
            {
                var addOns = entities.Select(entity => new AddOn
                {
                    Name = entity.Name,
                    AddOnCategoryId = entity.AddOnCategoryId,
                    AddOnUnitPrice = entity.AddOnUnitPrice,
                    IsActive = entity.IsActive ?? true,
                    HeadOfficeId = entity.HeadOfficeId,
                }).ToList();

                var response = await _repository.AddMultipleAsync(addOns);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AddOnVM entity)
        {
            try
            {
                var addOn = new AddOn
                {
                    Name = entity.Name,
                    AddOnCategoryId = entity.AddOnCategoryId,
                    AddOnUnitPrice = entity.AddOnUnitPrice,
                    IsActive = entity.IsActive ?? true,
                    HeadOfficeId = entity.HeadOfficeId
                };
                
                var response = await _repository.AddAsync(addOn);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] AddOnVM entity)
        {
            try
            {
                if (id != entity.Id)
                {
                    return BadRequest("AddOn ID mismatch");
                }

                var addOn = new AddOn
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    AddOnCategoryId = entity.AddOnCategoryId,
                    AddOnUnitPrice = entity.AddOnUnitPrice,
                    HeadOfficeId = entity.HeadOfficeId,
                    IsActive = entity.IsActive
                };

                var response = await _repository.UpdateAsync(addOn);
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