using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class ModifierCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public bool? IsRequired { get; set; }

    public bool? IsActive { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<Modifier> Modifiers { get; set; } = new List<Modifier>();
}
