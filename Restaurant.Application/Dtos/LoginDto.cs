using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class UserWithRolesDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<RoleTupleDto> Roles { get; set; } = new();
    }
    public record RoleTupleDto(int RoleId, string RoleName);
}
