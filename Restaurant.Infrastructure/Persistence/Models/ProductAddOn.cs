using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class ProductAddOn
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int AddOnId { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual AddOn AddOn { get; set; } = null!;

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual Product Product { get; set; } = null!;
}
