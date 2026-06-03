using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class DiscountBranch
{
    public int DiscountBranchId { get; set; }

    public int DiscountId { get; set; }

    public int BranchId { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Discount Discount { get; set; } = null!;
}
