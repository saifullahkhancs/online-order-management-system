using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class HeadOfficeDto
    {
        public int? HeadOfficeId { get; set; }
        public string? Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? BusinessCategory { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        //public int? AddressId { get; set; }
        public AddressDto? HeadOfficeAddress { get; set; }  
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }



}
