using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? Password { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public int? HeadOfficeId { get; set; }
        public string? HeadOfficeName { get; set; }

        public List<UserBranchDto> Branches { get; set; } = new();
        public List<UserRoleDto> Roles { get; set; } = new();
    }

    public class UserBranchDto
    {
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
    }

    public class UserRoleDto
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}
