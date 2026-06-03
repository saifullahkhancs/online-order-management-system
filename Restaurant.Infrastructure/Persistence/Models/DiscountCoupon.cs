using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class DiscountCoupon
{
    public int CouponId { get; set; }

    public int DiscountId { get; set; }

    public string Code { get; set; } = null!;

    public int? UsageLimit { get; set; }

    public int? PerUserLimit { get; set; }

    public bool IsPublic { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Discount Discount { get; set; } = null!;

    public virtual ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();
}
