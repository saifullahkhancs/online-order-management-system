using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderItemTaxis
{
    public long OrderItemTaxId { get; set; }

    public long? OrderItemId { get; set; }

    public int? TaxId { get; set; }

    public string? TaxName { get; set; }

    public decimal? TaxRate { get; set; }

    public decimal? TaxableAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public virtual OrderItem? OrderItem { get; set; }

    public virtual Tax? Tax { get; set; }
}
