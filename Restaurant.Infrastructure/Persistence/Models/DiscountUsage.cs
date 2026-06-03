using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class DiscountUsage
{
    public int DiscountUsageId { get; set; }

    public int DiscountId { get; set; }

    public int? CouponId { get; set; }

    public int? UserId { get; set; }

    public long OrderId { get; set; }

    public DateTime UsedDate { get; set; }

    public virtual DiscountCoupon? Coupon { get; set; }

    public virtual Discount Discount { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
