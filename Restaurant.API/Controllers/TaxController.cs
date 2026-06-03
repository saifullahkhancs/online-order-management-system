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
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage roles
    public class TaxController : ControllerBase
    {
        private readonly ITaxRepository _taxRepository;

        public TaxController(ITaxRepository taxRepository)
        {
            _taxRepository = taxRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _taxRepository.GetAllAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var response = await _taxRepository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaxRequest dto)
        {
            try
            {
                var response = await _taxRepository.CreateAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateTaxRequest dto)
        {
            try
            {
                var response = await _taxRepository.UpdateAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _taxRepository.DeleteAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }

        //[HttpPost("assign")]
        //public async Task<IActionResult> Assign([FromBody] AssignBranchTaxRequest dto)
        //{
        //    try
        //    {
        //        var response = await _taxRepository.AssignTaxToBranchAsync(dto);
        //        return StatusCode(response.StatusCode, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
        //    }
        //}

        //[HttpGet("branch/{branchId}")]
        //public async Task<IActionResult> GetBranchTaxes(int branchId)
        //{
        //    try
        //    {
        //        var response = await _taxRepository.GetBranchTaxesAsync(branchId);
        //        return StatusCode(response.StatusCode, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
        //    }
        //}
    }
}
