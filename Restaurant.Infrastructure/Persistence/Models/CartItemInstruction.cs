using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CartItemInstruction
{
    public int CartItemInstructionId { get; set; }

    public int? CartItemId { get; set; }

    public string? Instructions { get; set; }

    public virtual CartItem? CartItem { get; set; }
}
