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
    public class AddOnCategoryController : ControllerBase
    {
        private readonly IAddOnCategoryRepository _repository;
        
        public AddOnCategoryController(IAddOnCategoryRepository repository)
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
        public async Task<ActionResult> AddMultiple([FromBody] List<AddOnCategoryVM> entities)
        {
            try
            {
                var addOns = entities.Select(entity => new AddOnCategoryDto
                {
                    Name = entity.Name,
                    MinSelect = entity.MinSelect,
                    MaxSelect = entity.MaxSelect,
                    IsActive = entity.IsActive ?? true,
                    SortOrder = entity.SortOrder,
                    HeadOfficeId = entity.HeadOfficeId
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
        public async Task<ActionResult> Create([FromBody] AddOnCategoryVM entity)
        {
            try
            {
                var addOn = new AddOnCategoryDto
                {
                    Name = entity.Name,
                    MinSelect = entity.MinSelect,
                    MaxSelect = entity.MaxSelect,
                    IsActive = entity.IsActive ?? true,
                    SortOrder = entity.SortOrder,
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
        public async Task<ActionResult> Update(int id, [FromBody] AddOnCategoryVM entity)
        {
            try
            {
                if (id != entity.Id)
                {
                    return BadRequest("AddOn Category ID mismatch");
                }

                var addOn = new AddOnCategoryDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    MinSelect = entity.MinSelect,
                    MaxSelect = entity.MaxSelect,
                    IsActive = entity.IsActive ?? true,
                    SortOrder = entity.SortOrder,
                    HeadOfficeId = entity.HeadOfficeId
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