using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.Net;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage roles
    public class HeadOfficesController : ControllerBase
    {
        private readonly IHeadOfficeRepository _repository;

        public HeadOfficesController(IHeadOfficeRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<HeadOffice>>> GetAll()
        {
            try
            {
                var response = await _repository.GetAllAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<HeadOffice?>> GetById(int id)
        {
            try
            {
                var response = await _repository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HeadOfficeDto entity)
        {
            try
            {
                var response = await _repository.AddAsync(entity);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] HeadOfficeDto entity)
        {
            try
            {
                var response = await _repository.UpdateAsync(entity);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
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
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
