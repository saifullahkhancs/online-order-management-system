using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class BranchTaxis
{
    public int BranchTaxId { get; set; }

    public int? BranchId { get; set; }

    public int? TaxId { get; set; }

    public int? Priority { get; set; }

    public bool? IsCompound { get; set; }

    public bool? IsActive { get; set; }
}
