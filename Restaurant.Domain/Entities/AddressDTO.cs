using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public partial class AddressDTO
    {
        public int? AddressId { get; set; }

        public int? CustomerId { get; set; }

        public string AddressLine1 { get; set; } = null!;

        public string? AddressLine2 { get; set; }

        public string? AddressLine3 { get; set; }

        public string? City { get; set; } = null!;

        public string? State { get; set; }

        public string? Country { get; set; } = null!;

        public string? PostalCode { get; set; }

        public bool? IsDefault { get; set; }
    }
}
