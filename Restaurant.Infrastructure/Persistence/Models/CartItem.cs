using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int? CartId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Instructions { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<CartItemAddon> CartItemAddons { get; set; } = new List<CartItemAddon>();

    public virtual ICollection<CartItemModifier> CartItemModifiers { get; set; } = new List<CartItemModifier>();
}
