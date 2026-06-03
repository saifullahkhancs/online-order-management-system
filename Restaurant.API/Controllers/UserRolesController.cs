using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage roles
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRoleRepository _userRoleRepository;

        public UserRolesController(IUserRoleRepository userRoleRepository)
        {
            _userRoleRepository = userRoleRepository;
        }

        public record AssignRolesRequest(int UserId, List<int> RoleIds);

        
        [HttpGet("{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var roles = await _userRoleRepository.GetUserWithRolesAsync(userId);
            return Ok(roles);
        }

        [HttpPost("AssignUserRoles")]
        public async Task<IActionResult> AssignUserRoles([FromBody] AssignRolesRequest request)
        {
            var userRoles = await _userRoleRepository.AssignUserRoles(request.UserId, request.RoleIds);
            return Ok(userRoles);
        }
    }
}
