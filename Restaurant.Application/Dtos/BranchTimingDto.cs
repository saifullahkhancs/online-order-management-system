using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class BranchTimingDto
    {
        public int BranchTimingId { get; set; }
        public string? BranchTimingName { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public bool? IsClosed { get; set; }
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
