using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public int? BranchId { get; set; }

    public long? CustomerId { get; set; }

    public int? OrderTypeId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? DeliveryFee { get; set; }

    public decimal? TotalAmount { get; set; }

    public long? PaymentId { get; set; }

    public string? PaymentStatus { get; set; }

    public int? OrderStatusId { get; set; }

    public string? OrderStatus { get; set; }

    public long? DeliveryAddressId { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? DeliveryInstructions { get; set; }

    public Guid? GuestSessionToken { get; set; }

    public string? GuestName { get; set; }

    public string? GuestPhoneNumber { get; set; }

    public string? GuestEmailAddress { get; set; }

    public string? GuestAddressLine1 { get; set; }

    public string? GuestAddressLine2 { get; set; }

    public string? GuestAddressLine3 { get; set; }

    public string? GuestCity { get; set; }

    public string? GuestPostalCode { get; set; }

    public string? GuestCountry { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public int? TableId { get; set; }

    public virtual ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; } = new List<OrderDiscount>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<OrderTaxis> OrderTaxes { get; set; } = new List<OrderTaxis>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Table? Table { get; set; }
}
