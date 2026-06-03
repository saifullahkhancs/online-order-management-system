using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Branch
{
    public int BranchId { get; set; }

    public int? HeadOfficeId { get; set; }

    public string? BranchName { get; set; }

    public int? AddressId { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();

    public virtual ICollection<BranchTiming> BranchTimings { get; set; } = new List<BranchTiming>();

    public virtual ICollection<DiscountBranch> DiscountBranches { get; set; } = new List<DiscountBranch>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
}
