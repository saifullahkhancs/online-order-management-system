using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities
{
    public class BranchTiming
    {
        public int BranchTimingId { get; set; }
        public int BranchId { get; set; }
        public string? BranchTimingName { get; set; } // e.g., Monday, Friday
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
