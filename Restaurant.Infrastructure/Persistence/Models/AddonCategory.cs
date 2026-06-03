using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class AddOnCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? MinSelect { get; set; }

    public int? MaxSelect { get; set; }

    public bool? IsActive { get; set; }

    public int? SortOrder { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual ICollection<AddOn> AddOns { get; set; } = new List<AddOn>();

    public virtual HeadOffice? HeadOffice { get; set; }
}
