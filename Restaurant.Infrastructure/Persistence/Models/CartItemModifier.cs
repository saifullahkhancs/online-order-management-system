using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CartItemModifier
{
    public int CartItemModifierId { get; set; }

    public int? CartItemId { get; set; }

    public int? ModifierCategoryId { get; set; }

    public int? ModifierId { get; set; }

    public string? ModifierName { get; set; }

    public decimal? ModifierPrice { get; set; }

    public virtual CartItem? CartItem { get; set; }
}
