using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class OrderNotification
{
    public long OrderNotificationId { get; set; }

    public long? OrderId { get; set; }

    public int? NotificationTypeId { get; set; }

    public string? NotificationType { get; set; }

    public int? NotificationPriority { get; set; }

    public string? MessageText { get; set; }

    public bool? IsSent { get; set; }

    public DateTime? SentAt { get; set; }
}
