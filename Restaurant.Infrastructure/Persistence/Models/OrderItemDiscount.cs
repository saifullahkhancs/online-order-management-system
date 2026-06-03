using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderItemDiscount
{
    public long OrderItemDiscountId { get; set; }

    public long? OrderItemId { get; set; }

    public int? DiscountId { get; set; }

    public string? DiscountName { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? DiscountType { get; set; }

    public virtual Discount? Discount { get; set; }

    public virtual OrderItem? OrderItem { get; set; }
}
