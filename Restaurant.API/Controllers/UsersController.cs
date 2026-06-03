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
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly UserContextService _userContext;

        public UsersController(IUserRepository userRepository,UserContextService userContext)
        {
            _userRepository = userRepository;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            try
            {
                var response = await _userRepository.GetAllAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
           try
            {
                var response = await _userRepository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(UserDto dto)
        {
            try
            {
                var response = await _userRepository.AddAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(int id, UserDto dto)
        {
            try
            {
                var response = await _userRepository.UpdateAsync(dto);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _userRepository.DeleteAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException.ToString());
            }
        }

        [AllowAnonymous]
        [Authorize] // anyone authenticated
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                var loggedInUserId = _userContext.GetUserId();
                var loggedInRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                bool isPrivileged = loggedInRoles.Contains("SystemAdmin")
                                    || loggedInRoles.Contains("SuperAdmin")
                                    || loggedInRoles.Contains("Admin");

                // Rule: Normal user can only reset their own password
                if (!isPrivileged && request.UserId != loggedInUserId)
                {
                    return Forbid(); // 403 Forbidden
                }

                var response = await _userRepository.ResetPassword(request);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
