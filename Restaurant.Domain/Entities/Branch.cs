using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public class Branch
    {
        public int BranchId { get; set; }
        public int HeadOfficeId { get; set; }
        public string? BranchName { get; set; }
        public int? AddressId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // New navigation objects
        public AddressDTO? BranchAddress { get; set; }
        public List<BranchTiming>? BranchTimings { get; set; }
    }
}
