using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderItemAddon
{
    public long OrderItemAddonId { get; set; }

    public long? OrderItemId { get; set; }

    public int? AddonId { get; set; }

    public string? AddonName { get; set; }

    public int? AddonQuantity { get; set; }

    public decimal? AddonPrice { get; set; }

    public int? AddonCategoryId { get; set; }

    public virtual OrderItem? OrderItem { get; set; }
}
