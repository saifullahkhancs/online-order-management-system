using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class HeadOffice
{
    public int HeadOfficeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public int? AddressId { get; set; }

    public string? BusinessCategory { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AddOnCategory> AddOnCategories { get; set; } = new List<AddOnCategory>();

    public virtual ICollection<AddOn> AddOns { get; set; } = new List<AddOn>();

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public virtual ICollection<ModifierCategory> ModifierCategories { get; set; } = new List<ModifierCategory>();

    public virtual ICollection<Modifier> Modifiers { get; set; } = new List<Modifier>();

    public virtual ICollection<ProductAddOn> ProductAddOns { get; set; } = new List<ProductAddOn>();

    public virtual ICollection<ProductModifier> ProductModifiers { get; set; } = new List<ProductModifier>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();

    public virtual ICollection<Tax> Taxes { get; set; } = new List<Tax>();
}
