using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public class BranchProductVM
    {
        public int BranchProductId { get; set; }
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        // Navigatio
    }
}