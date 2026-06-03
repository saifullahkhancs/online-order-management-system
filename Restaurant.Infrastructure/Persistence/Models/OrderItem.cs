using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderItem
{
    public long OrderItemId { get; set; }

    public long? OrderId { get; set; }

    public int? ProductId { get; set; }

    public string? ProductName { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? FinalPrice { get; set; }

    public string? Instructions { get; set; }

    public decimal? DiscountAmount { get; set; }

    public virtual Order? Order { get; set; }

    public virtual ICollection<OrderItemAddon> OrderItemAddons { get; set; } = new List<OrderItemAddon>();

    public virtual ICollection<OrderItemDiscount> OrderItemDiscounts { get; set; } = new List<OrderItemDiscount>();

    public virtual ICollection<OrderItemModifier> OrderItemModifiers { get; set; } = new List<OrderItemModifier>();

    public virtual ICollection<OrderItemTaxis> OrderItemTaxes { get; set; } = new List<OrderItemTaxis>();

    public virtual Product? Product { get; set; }
}
