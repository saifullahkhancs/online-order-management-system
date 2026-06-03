using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderTaxis
{
    public long OrderTaxId { get; set; }

    public long? OrderId { get; set; }

    public int? TaxId { get; set; }

    public string? TaxName { get; set; }

    public decimal? TaxRate { get; set; }

    public decimal? TaxableAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Tax? Tax { get; set; }
}
