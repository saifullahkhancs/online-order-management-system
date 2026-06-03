using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderStatusTransitionsRule
{
    public int Id { get; set; }

    public int OrderTypeId { get; set; }

    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public string? RequiredRole { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
