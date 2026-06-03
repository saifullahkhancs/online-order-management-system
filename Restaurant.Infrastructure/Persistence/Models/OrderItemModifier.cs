using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderItemModifier
{
    public long OrderItemModifierId { get; set; }

    public long? OrderItemId { get; set; }

    public int? ModifierId { get; set; }

    public string? ModifierName { get; set; }

    public decimal? ModifierPrice { get; set; }

    public virtual OrderItem? OrderItem { get; set; }
}
