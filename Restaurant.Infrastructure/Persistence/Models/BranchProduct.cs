using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class BranchProduct
{
    public int BranchProductId { get; set; }

    public int BranchId { get; set; }

    public int ProductId { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
