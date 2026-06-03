using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class PaymentStatus
{
    public int PaymentStatusId { get; set; }

    public string Name { get; set; } = null!;
}
