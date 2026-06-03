using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class AddOn
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int AddOnCategoryId { get; set; }

    public decimal? AddOnUnitPrice { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual AddOnCategory AddOnCategory { get; set; } = null!;

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<ProductAddOn> ProductAddOns { get; set; } = new List<ProductAddOn>();
}
