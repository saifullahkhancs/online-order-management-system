using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Tax
{
    public int TaxId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public decimal? Rate { get; set; }

    public bool? IsPercentage { get; set; }

    public bool? IsCompound { get; set; }

    public int? Priority { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public int? PaymentTypeId { get; set; }

    public string? PaymentType { get; set; }

    public int HeadOfficeId { get; set; }

    public virtual HeadOffice HeadOffice { get; set; } = null!;

    public virtual ICollection<OrderItemTaxis> OrderItemTaxes { get; set; } = new List<OrderItemTaxis>();

    public virtual ICollection<OrderTaxis> OrderTaxes { get; set; } = new List<OrderTaxis>();
}
