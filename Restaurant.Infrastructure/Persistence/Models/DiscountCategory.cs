using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class DiscountCategory
{
    public int DiscountCategoryId { get; set; }

    public int DiscountId { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Discount Discount { get; set; } = null!;
}
