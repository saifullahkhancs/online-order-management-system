using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class OrderDto
    {
        public long? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public long? CustomerId { get; set; }
        public int? HeadOfficeId { get; set; }
        public int BranchId { get; set; }
        public int? OrderTypeId { get; set; }
        public string? OrderType { get; set; } // delivery | pickup | dinein
        public DateTime? OrderDate { get; set; }
        public int? TableId { get; set; }
        
        public decimal? Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DeliveryFee { get; set; }
        public decimal? TotalAmount { get; set; }

        //public int? PaymentId { get; set; }
        public string? PaymentStatus { get; set; }

        //public int? OrderStatusId { get; set; }
        public string? OrderStatus { get; set; }

        public int? DeliveryAddressId { get; set; }
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
        //public int? CreatedBy {get; set;}
        public DateTime? UpdatedAt { get; set; }
        //public int? UpdatedBy {get; set;}

        //public bool? IsActive { get; set; }
        //public bool? IsDeleted { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderTax> OrderTaxes { get; set; } = new List<OrderTax>();
        //public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; } = new List<OrderDiscount>();

    }

    public class OrderItem
    {
        public long? OrderItemId { get; set; }
        public long? OrderId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? FinalPrice { get; set; }
        public string? Instructions { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? ImageUrl { get; set; }

        public List<OrderItemModifier> ItemModifiers { get; set; } = new();
        public List<OrderItemAddon> ItemAddons { get; set; } = new();
        //public List<OrderItemDiscount> ItemDiscounts { get; set; } = new();
        //public List<OrderItemTax> ItemTax { get; set; } = new();
    }
    public class OrderItemModifier
    {
        public long? OrderItemModifierId { get; set; }
        public long? OrderItemId { get; set; }
        public int? ModifierId { get; set; }
        public string? ModifierName { get; set; }
        public decimal? ModifierPrice { get; set; }
    }
    public class OrderItemAddon
    {
        public long? OrderItemAddonId { get; set; }
        public long? OrderItemId { get; set; }
        public int? AddonId { get; set; }
        public string? AddonName { get; set; }
        public int? AddonQuantity { get; set; }
        public decimal? AddonPrice { get; set; }
        public int? AddonCategoryId { get; set; }
    }
    public class OrderItemDiscount
    {
        public long? OrderItemDiscountId { get; set; }
        public long? OrderItemId { get; set; }
        public int? DiscountId { get; set; }
        public string? DiscountName { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? DiscountType { get; set; }
    }
    public class OrderItemTax
    {
        public long? OrderItemTaxId { get; set; }
        public long? OrderItemId { get; set; }
        public int? TaxId { get; set; }
        public string? TaxName { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
    }

    public class OrderTax
    {
        public long? OrderTaxId { get; set; }
        public long? OrderId { get; set; }
        public int? TaxId { get; set; }
        public string? TaxName { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
    }
    public class OrderDiscount
    {
        public long? OrderDiscountId { get; set; }
        public long? OrderId { get; set; }
        public int? DiscountId { get; set; }
        public string? DiscountCode { get; set; }
        public string? DiscountName { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? DiscountType { get; set; }
    }

    public class PlaceOrderResponseDto
    {
        public long? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? OrderStatus { get; set; }
        public string? OrderType { get; set; }
    }

    public class OrderStatusDto
    {
        public long? OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class CancelOrderRequest
    {
        public Guid? GuestSessionToken { get; set; }
        public string? CancelReason { get; set; }
    }

    #region ----------------------- Manage Orders API ----------------------------
    public class LiveOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public long? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
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

        public DateTime CreatedAt { get; set; }

        public BranchDto? Branch { get; set; }
        public HeadOfficeDto? HeadOffice { get; set; }
        public List<LiveOrderItemDto> Items { get; set; } = new();
    }

    public class LiveOrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<ModifierDto> Modifiers { get; set; } = new();
        public List<AddonDto> Addons { get; set; } = new();
    }

    public class ModifierDto
    {
        //public long? OrderItemModifierId { get; set; }
        //public long? OrderItemId { get; set; }
        //public int? ModifierId { get; set; }
        public string? ModifierName { get; set; }
        public decimal? ModifierPrice { get; set; }
    }

    public class AddonDto
    {
        //public long? OrderItemAddonId { get; set; }
        //public long? OrderItemId { get; set; }
        //public int? AddonId { get; set; }
        public string? AddonName { get; set; }
        public int? AddonQuantity { get; set; }
        public decimal? AddonPrice { get; set; }
        //public int? AddonCategoryId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public int NewStatusId { get; set; }
        public string? Remarks { get; set; }
    }

    public class AvailableStatusOption
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
    }

    public class OrderStatusTransitionRule
    {
        public int Id { get; set; }
        public int? OrderTypeId { get; set; }       // 1 = Delivery, 2 = Pickup, NULL = All
        public int FromStatusId { get; set; }
        public int ToStatusId { get; set; }
        public string? RequiredRole { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    #endregion
}
