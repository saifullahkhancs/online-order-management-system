using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CategoryProduct
{
    public int CategoryProductId { get; set; }

    public int CategoryId { get; set; }

    public int ProductId { get; set; }

    public bool IsActive { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
