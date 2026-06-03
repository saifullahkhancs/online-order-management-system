using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderDiscount
{
    public long OrderDiscountId { get; set; }

    public long? OrderId { get; set; }

    public int? DiscountId { get; set; }

    public string? DiscountCode { get; set; }

    public string? DiscountName { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? DiscountType { get; set; }

    public virtual Discount? Discount { get; set; }

    public virtual Order? Order { get; set; }
}
