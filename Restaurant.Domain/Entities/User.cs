using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }

        public int? HeadOfficeId { get; set; }
        public string? HeadOfficeName { get; set; }
        public HeadOffice? HeadOffice { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<Role> Roles { get; set; }
        public ICollection<Branch> Branches { get; set; }
    }
}
