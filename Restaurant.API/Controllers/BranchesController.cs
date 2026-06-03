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
    // [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage Branches
    public class BranchesController : ControllerBase
    {
        private readonly IBranchRepository _repository;

        public BranchesController(IBranchRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var response = await _repository.GetAllAsync();
                return StatusCode(response.StatusCode,response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Branch?>> GetById(int id)
        {
            try
            {
                var response = await _repository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Branch entity)
        {
            try
            {
                var response = await _repository.AddAsync(entity);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] Branch entity)
        {
            try
            {
                var response = await _repository.UpdateAsync(entity);
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
