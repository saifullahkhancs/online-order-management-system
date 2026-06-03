using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public class BranchProduct
    {
        public int BranchProductId { get; set; }
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public Branch? Branch { get; set; }
        public Product? Product { get; set; }
    }
}