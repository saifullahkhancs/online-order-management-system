using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderStatus
{
    public int OrderStatusId { get; set; }

    public string Name { get; set; } = null!;

    public bool? IsTerminal { get; set; }

    public virtual ICollection<OrderStatusHistory> OrderStatusHistoryNewOrderStatuses { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistoryOldOrderStatuses { get; set; } = new List<OrderStatusHistory>();
}
