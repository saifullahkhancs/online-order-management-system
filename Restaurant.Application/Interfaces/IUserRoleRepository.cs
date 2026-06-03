using Restaurant.Application.Dtos;
using Restaurant.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<UserWithRolesDto> AssignUserRoles(int userId, IEnumerable<int> roleIds);
        Task<UserWithRolesDto?> GetUserWithRolesAsync(int userId);
    }
}
