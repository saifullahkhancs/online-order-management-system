using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class BranchDto
    {
        public int BranchId { get; set; }
        public int HeadOfficeId { get; set; }
        public string? HeadOfficeName { get; set; }
        public string? BranchName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public AddressDto? BranchAddress { get; set; }
        public HeadOfficeDto? HeadOffice { get; set; }
        public List<BranchTimingDto>? BranchTimings { get; set; }
    }

    
}
