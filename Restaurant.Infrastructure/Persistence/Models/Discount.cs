using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Discount
{
    public int DiscountId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DiscountType { get; set; }

    public decimal Value { get; set; }

    public int AppliesTo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public decimal? MinimumOrderAmount { get; set; }

    public decimal? MaximumDiscountAmount { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? HeadOfficeId { get; set; }

    public virtual ICollection<DiscountBranch> DiscountBranches { get; set; } = new List<DiscountBranch>();

    public virtual ICollection<DiscountCategory> DiscountCategories { get; set; } = new List<DiscountCategory>();

    public virtual ICollection<DiscountCoupon> DiscountCoupons { get; set; } = new List<DiscountCoupon>();

    public virtual ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();

    public virtual ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();

    public virtual HeadOffice? HeadOffice { get; set; }

    public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; } = new List<OrderDiscount>();

    public virtual ICollection<OrderItemDiscount> OrderItemDiscounts { get; set; } = new List<OrderItemDiscount>();
}
