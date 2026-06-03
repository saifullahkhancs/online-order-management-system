using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderType
{
    public int OrderTypeId { get; set; }

    public string Name { get; set; } = null!;
}
