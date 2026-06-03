using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class CartDto
    {
        public int CartId { get; set; }
        public int? UserId { get; set; }
        public Guid? GuestSessionToken { get; set; }
        public int BranchId { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public List<TaxDto> Taxes { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Instructions { get; set; }

        public List<CartItemModifierDto> Modifiers { get; set; } = new();
        public List<CartItemAddonDto> Addons { get; set; } = new();
    }

    public class CartItemModifierDto
    {
        public int ModifierId { get; set; }
        public string ModifierName { get; set; } = string.Empty;
        public decimal ModifierPrice { get; set; }
    }

    public class CartItemAddonDto
    {
        public int AddOnId { get; set; }
        public string AddOnName { get; set; } = string.Empty;
        public decimal AddOnPrice { get; set; }
        public int AddOnQuantity { get; set; }
    }

    public class CartItemInstructionDto
    {
        public int CartItemInstructionId { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }

    //public class TaxDto
    //{
    //    public string TaxName { get; set; } = string.Empty;
    //    public decimal TaxRate { get; set; }
    //    public bool IsPercentage { get; set; }
    //    public string PaymentType { get; set; } = string.Empty;
    //    public decimal Amount { get; set; }
    //}


    // INPUT DTOs
    public class AddCartItemRequest
    {
        public int? UserId { get; set; }
        public Guid? GuestSessionToken { get; set; }
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Instructions { get; set; }

        public List<AddCartItemModifierRequest> Modifiers { get; set; } = new();
        public List<AddCartItemAddonRequest> Addons { get; set; } = new();
    }

    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class AddCartItemModifierRequest
    {
        public int ModifierId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class AddCartItemAddonRequest
    {
        public int AddOnId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
