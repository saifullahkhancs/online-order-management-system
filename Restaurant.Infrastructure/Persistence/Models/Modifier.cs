using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Modifier
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal DefaultPrice { get; set; }

    public int? HeadOfficeId { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ModifierCategory Category { get; set; } = null!;

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<ProductModifier> ProductModifiers { get; set; } = new List<ProductModifier>();
}
