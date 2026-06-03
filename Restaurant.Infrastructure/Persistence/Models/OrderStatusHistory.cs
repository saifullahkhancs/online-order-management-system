using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderStatusHistory
{
    public long OrderStatusHistoryId { get; set; }

    public long? OrderId { get; set; }

    public int? OldOrderStatusId { get; set; }

    public int? NewOrderStatusId { get; set; }

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public DateTime? ChangedAt { get; set; }

    public string? ChangedByUsername { get; set; }

    public string? Remarks { get; set; }

    public int? ChangedByUserId { get; set; }

    public bool? IsVisibleToCustomer { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual OrderStatus? NewOrderStatus { get; set; }

    public virtual OrderStatus? OldOrderStatus { get; set; }

    public virtual Order? Order { get; set; }
}
