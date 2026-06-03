using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Table
{
    public int TableId { get; set; }

    public string? TableName { get; set; }

    public string? TableNumber { get; set; }

    public string? TableLocation { get; set; }

    public int? TableCapacity { get; set; }

    public string? TableStatus { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public int? HeadOfficeId { get; set; }

    public int? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
