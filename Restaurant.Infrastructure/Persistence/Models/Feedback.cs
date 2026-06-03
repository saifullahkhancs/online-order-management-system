using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Feedback
{
    public long FeedbackId { get; set; }

    public long? OrderId { get; set; }

    public int? CustomerId { get; set; }

    public Guid? GuestSessionToken { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? BranchId { get; set; }

    public string? FeedbackSource { get; set; }

    public bool? IsResolved { get; set; }

    public int? ResolvedBy { get; set; }

    public string? ResolutionNote { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Order? Order { get; set; }
}
