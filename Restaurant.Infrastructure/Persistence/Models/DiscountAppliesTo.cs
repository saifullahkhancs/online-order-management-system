using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class DiscountAppliesTo
{
    public int DiscountAppliesToId { get; set; }

    public string Name { get; set; } = null!;
}
