using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class ProductModifier
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int ModifierId { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual Modifier Modifier { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
