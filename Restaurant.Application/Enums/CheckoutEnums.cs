using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Enums
{
    // Flow                    Status Progression
    // Delivery(Registered)   Pending → Confirmed → Preparing → OutForDelivery → Delivered
    // Delivery(Guest)        Pending → Confirmed → Preparing → OutForDelivery → Delivered
    // Pickup(Registered)     Pending → Confirmed → Preparing → ReadyForPickup → Completed
    // Pickup(Guest)          Pending → Confirmed → Preparing → ReadyForPickup → Completed
    public enum OrderType
    {
        Delivery = 1,
        Pickup = 2,
        DineIn = 3,
        Takeaway = 4
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        Online = 3,      // e.g. Stripe, PayFast, etc.
        Wallet = 4,      // Internal loyalty or wallet credits
        Split = 5        // Part cash, part card
    }

    public enum PaymentStatus
    {
        Pending = 1,      // Awaiting confirmation (cash not received yet)
        Authorized = 2,   // For card/online payments authorized
        Paid = 3,         // Payment fully received
        Failed = 4,       // Payment attempt failed
        Refunded = 5,     // Money refunded
        Cancelled = 6     // Payment cancelled before confirmation
    }

    public enum OrderStatus
    {
        Pending = 1,           // Just created, waiting for confirmation
        Confirmed = 2,         // Confirmed by system or staff
        Preparing = 3,         // Kitchen preparing the order
        ReadyForPickup = 4,    // For pickup orders – ready at counter
        OutForDelivery = 5,    // For delivery orders – assigned to driver
        Delivered = 6,         // Delivery completed
        Completed = 7,         // Pickup collected or dine-in completed
        Cancelled = 8,         // Cancelled by customer or admin
        Rejected = 9,          // Rejected by branch/staff
        Refunded = 10          // Order refunded
    }

    public enum DiscountAppliesTo
    {
        Restaurant = 1,        // Applies to entire restaurant (all branches, all products)
        Branch = 2,            // Applies to specific branch(es)
        Product = 3,           // Applies to specific product(s)
        Category = 4,          // Applies to specific category(ies)
        Coupon = 5             // Applies when a coupon code is used
    }
}
