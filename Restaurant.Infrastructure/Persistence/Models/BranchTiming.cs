using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class BranchTiming
{
    public int BranchTimingId { get; set; }

    public int? BranchId { get; set; }

    public string? BranchTimingName { get; set; }

    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }

    public bool? IsClosed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public int? SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual Branch? Branch { get; set; }
}
