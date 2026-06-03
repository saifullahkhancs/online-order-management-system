using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CartItemAddon
{
    public int CartItemAddonId { get; set; }

    public int? CartItemId { get; set; }

    public int? AddOnId { get; set; }

    public string? AddOnName { get; set; }

    public decimal? AddOnPrice { get; set; }

    public int? AddOnQuantity { get; set; }

    public int? AddonCategoryId { get; set; }

    public virtual CartItem? CartItem { get; set; }
}
