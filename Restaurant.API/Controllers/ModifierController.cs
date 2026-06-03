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
    public class ModifierController : ControllerBase
    {
        private readonly IModifierRepository _repository;
        
        public ModifierController(IModifierRepository repository)
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
        public async Task<ActionResult> GetById(int id)
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
        public async Task<ActionResult> AddMultiple([FromBody] List<ModifierVM> entities)
        {
            try
            {
                var modifiers = entities.Select(entity => new Modifier
                {
                    Name = entity.Name,
                    CategoryId = entity.CategoryId,
                    DefaultPrice = entity.DefaultPrice,
                    HeadOfficeId = entity.HeadOfficeId,
                    IsActive = entity.IsActive,
                }).ToList();

                var response = await _repository.AddMultipleAsync(modifiers);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ModifierVM entity)
        {
            try
            {
                var modifier = new Modifier
                {
                    Name = entity.Name,
                    CategoryId = entity.CategoryId,
                    DefaultPrice = entity.DefaultPrice,
                    HeadOfficeId = entity.HeadOfficeId,
                    IsActive = entity.IsActive,
                };
                
                var response = await _repository.AddAsync(modifier);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] ModifierVM entity)
        {
            try
            {
                if (id != entity.Id)
                {
                    return BadRequest("Modifier ID mismatch");
                }

                var modifier = new Modifier
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    CategoryId = entity.CategoryId,
                    DefaultPrice = entity.DefaultPrice,
                    HeadOfficeId = entity.HeadOfficeId  ,
                    IsActive = entity.IsActive,
                };

                var response = await _repository.UpdateAsync(modifier);
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