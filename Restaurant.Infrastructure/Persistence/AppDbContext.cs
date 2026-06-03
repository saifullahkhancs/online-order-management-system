using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Restaurant.Infrastructure.Persistence.Models;

namespace Restaurant.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AddOn> AddOns { get; set; }

    public virtual DbSet<AddOnCategory> AddOnCategories { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchProduct> BranchProducts { get; set; }

    public virtual DbSet<BranchTaxis> BranchTaxes { get; set; }

    public virtual DbSet<BranchTiming> BranchTimings { get; set; }

    public virtual DbSet<CardMachine> CardMachines { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<CartItemAddon> CartItemAddons { get; set; }

    public virtual DbSet<CartItemModifier> CartItemModifiers { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategoryProduct> CategoryProducts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<DiscountAppliesTo> DiscountAppliesTos { get; set; }

    public virtual DbSet<DiscountBranch> DiscountBranches { get; set; }

    public virtual DbSet<DiscountCategory> DiscountCategories { get; set; }

    public virtual DbSet<DiscountCoupon> DiscountCoupons { get; set; }

    public virtual DbSet<DiscountProduct> DiscountProducts { get; set; }

    public virtual DbSet<DiscountUsage> DiscountUsages { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<HeadOffice> HeadOffices { get; set; }

    public virtual DbSet<Modifier> Modifiers { get; set; }

    public virtual DbSet<ModifierCategory> ModifierCategories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDiscount> OrderDiscounts { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderItemAddon> OrderItemAddons { get; set; }

    public virtual DbSet<OrderItemDiscount> OrderItemDiscounts { get; set; }

    public virtual DbSet<OrderItemModifier> OrderItemModifiers { get; set; }

    public virtual DbSet<OrderItemTaxis> OrderItemTaxes { get; set; }

    public virtual DbSet<OrderNotification> OrderNotifications { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OrderStatusTransitionsRule> OrderStatusTransitionsRules { get; set; }

    public virtual DbSet<OrderTaxis> OrderTaxes { get; set; }

    public virtual DbSet<OrderType> OrderTypes { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentStatus> PaymentStatuses { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAddOn> ProductAddOns { get; set; }

    public virtual DbSet<ProductModifier> ProductModifiers { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    public virtual DbSet<Tax> Taxs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBranch> UserBranches { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddOn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AddOn__3214EC07A6FA7E36");

            entity.ToTable("AddOn");

            entity.Property(e => e.AddOnUnitPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.AddOnCategory).WithMany(p => p.AddOns)
                .HasForeignKey(d => d.AddOnCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AddOn_AddOnCategory");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.AddOns)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_AddOn_HeadOffice");
        });

        modelBuilder.Entity<AddOnCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AddOnCat__3214EC078C95D93E");

            entity.ToTable("AddOnCategory");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxSelect).HasDefaultValue(5);
            entity.Property(e => e.MinSelect).HasDefaultValue(0);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.AddOnCategories)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_AddOnCategory_HeadOffice");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.Property(e => e.AddressLine1).HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasMaxLength(255);
            entity.Property(e => e.AddressLine3).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(e => e.BranchName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Branches)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_Branches_HeadOffice");
        });

        modelBuilder.Entity<BranchProduct>(entity =>
        {
            entity.HasKey(e => e.BranchProductId).HasName("PK__BranchPr__190F7B5BD79CE326");

            entity.ToTable("BranchProduct");

            entity.HasIndex(e => new { e.BranchId, e.ProductId }, "UQ_BranchProduct_Branch_Product").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchProducts)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchProduct_Branches");

            entity.HasOne(d => d.Product).WithMany(p => p.BranchProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchProduct_Products");
        });

        modelBuilder.Entity<BranchTaxis>(entity =>
        {
            entity.HasKey(e => e.BranchTaxId);
        });

        modelBuilder.Entity<BranchTiming>(entity =>
        {
            entity.Property(e => e.BranchTimingName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchTimings)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_BranchTimings_Branches");
        });

        modelBuilder.Entity<CardMachine>(entity =>
        {
            entity.Property(e => e.CardMachineId).ValueGeneratedNever();
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.MachineName).HasMaxLength(200);
            entity.Property(e => e.MachineStatus).HasMaxLength(100);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.GuestSessionToken }, "UQ_Carts_UserId_GuestSessionToken").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => e.CartItemId, "IX_CartItems");

            entity.Property(e => e.Instructions).HasMaxLength(500);
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([UnitPrice]*[Quantity])", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Cart");
        });

        modelBuilder.Entity<CartItemAddon>(entity =>
        {
            entity.Property(e => e.AddOnName).HasMaxLength(255);
            entity.Property(e => e.AddOnPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.CartItem).WithMany(p => p.CartItemAddons)
                .HasForeignKey(d => d.CartItemId)
                .HasConstraintName("FK_CartItemAddons_CartItems");
        });

        modelBuilder.Entity<CartItemModifier>(entity =>
        {
            entity.Property(e => e.ModifierName).HasMaxLength(255);
            entity.Property(e => e.ModifierPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.CartItem).WithMany(p => p.CartItemModifiers)
                .HasForeignKey(d => d.CartItemId)
                .HasConstraintName("FK_CartItemModifiers_CartItems");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B2F35A8B0");

            entity.Property(e => e.CategoryName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Categories)
                .HasForeignKey(d => d.HeadOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categories_HeadOffice");
        });

        modelBuilder.Entity<CategoryProduct>(entity =>
        {
            entity.HasKey(e => e.CategoryProductId).HasName("PK__Category__FAFA184FEEC5AAC3");

            entity.ToTable("CategoryProduct");

            entity.HasIndex(e => new { e.CategoryId, e.ProductId }, "UQ_Category_Product").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryProducts)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryProduct_Category");

            entity.HasOne(d => d.Product).WithMany(p => p.CategoryProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryProduct_Product");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.LoyaltyNumber).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.DiscountId).HasName("PK__Discount__E43F6D9646621B2A");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaximumDiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinimumOrderAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Value).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Discounts)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_Discounts_HeadOffice");
        });

        modelBuilder.Entity<DiscountAppliesTo>(entity =>
        {
            entity.HasKey(e => e.DiscountAppliesToId).HasName("PK__Discount__1CD7B727F6167247");

            entity.ToTable("DiscountAppliesTo");

            entity.Property(e => e.DiscountAppliesToId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<DiscountBranch>(entity =>
        {
            entity.HasKey(e => e.DiscountBranchId).HasName("PK__Discount__FCB0183851209747");

            entity.HasOne(d => d.Branch).WithMany(p => p.DiscountBranches)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountB__Branc__5A846E65");

            entity.HasOne(d => d.Discount).WithMany(p => p.DiscountBranches)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountB__Disco__59904A2C");
        });

        modelBuilder.Entity<DiscountCategory>(entity =>
        {
            entity.HasKey(e => e.DiscountCategoryId).HasName("PK__Discount__25BD2140C2F1D61C");

            entity.HasOne(d => d.Category).WithMany(p => p.DiscountCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountC__Categ__6225902D");

            entity.HasOne(d => d.Discount).WithMany(p => p.DiscountCategories)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountC__Disco__61316BF4");
        });

        modelBuilder.Entity<DiscountCoupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Discount__384AF1BA6378C01B");

            entity.HasIndex(e => e.Code, "UQ__Discount__A25C5AA7264F4835").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsPublic).HasDefaultValue(true);

            entity.HasOne(d => d.Discount).WithMany(p => p.DiscountCoupons)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountC__Disco__67DE6983");
        });

        modelBuilder.Entity<DiscountProduct>(entity =>
        {
            entity.HasKey(e => e.DiscountProductId).HasName("PK__Discount__0B7AC54D6965C34D");

            entity.HasOne(d => d.Discount).WithMany(p => p.DiscountProducts)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountP__Disco__5D60DB10");

            entity.HasOne(d => d.Product).WithMany(p => p.DiscountProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountP__Produ__5E54FF49");
        });

        modelBuilder.Entity<DiscountUsage>(entity =>
        {
            entity.HasKey(e => e.DiscountUsageId).HasName("PK__Discount__C5D41D63D315E6AF");

            entity.ToTable("DiscountUsage");

            entity.Property(e => e.UsedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Coupon).WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK__DiscountU__Coupo__6CA31EA0");

            entity.HasOne(d => d.Discount).WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountU__Disco__6BAEFA67");

            entity.HasOne(d => d.Order).WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiscountU__Order__6D9742D9");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.FeedbackSource).HasMaxLength(100);

            entity.HasOne(d => d.Branch).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_Feedbacks_Branches");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Feedbacks_Customers");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Feedbacks_Orders");
        });

        modelBuilder.Entity<HeadOffice>(entity =>
        {
            entity.HasKey(e => e.HeadOfficeId).HasName("PK_Restaurants");

            entity.ToTable("HeadOffice");

            entity.Property(e => e.BusinessCategory).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Website).HasMaxLength(200);
        });

        modelBuilder.Entity<Modifier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modifier__3214EC07DF291039");

            entity.ToTable("Modifier");

            entity.Property(e => e.DefaultPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.Modifiers)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Modifier_ModifierCategory");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Modifiers)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_Modifier_HeadOffice");
        });

        modelBuilder.Entity<ModifierCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modifier__3214EC076D5A0DE6");

            entity.ToTable("ModifierCategory");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsRequired).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.ModifierCategories)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_ModifierCategory_HeadOffice");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DeliveryInstructions).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.GuestAddressLine1).HasMaxLength(500);
            entity.Property(e => e.GuestAddressLine2).HasMaxLength(500);
            entity.Property(e => e.GuestAddressLine3).HasMaxLength(500);
            entity.Property(e => e.GuestCity).HasMaxLength(100);
            entity.Property(e => e.GuestCountry).HasMaxLength(50);
            entity.Property(e => e.GuestEmailAddress).HasMaxLength(255);
            entity.Property(e => e.GuestName).HasMaxLength(255);
            entity.Property(e => e.GuestPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.GuestPostalCode).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.OrderNumber).HasMaxLength(100);
            entity.Property(e => e.OrderStatus).HasMaxLength(255);
            entity.Property(e => e.PaymentStatus).HasMaxLength(255);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Table).WithMany(p => p.Orders)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("FK_Orders_Table");
        });

        modelBuilder.Entity<OrderDiscount>(entity =>
        {
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DiscountCode).HasMaxLength(50);
            entity.Property(e => e.DiscountName).HasMaxLength(150);
            entity.Property(e => e.DiscountType).HasMaxLength(20);

            entity.HasOne(d => d.Discount).WithMany(p => p.OrderDiscounts)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK_OrderDiscounts_Discounts");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDiscounts)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderDiscounts_Orders");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FinalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Instructions).HasMaxLength(500);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<OrderItemAddon>(entity =>
        {
            entity.Property(e => e.AddonName).HasMaxLength(255);
            entity.Property(e => e.AddonPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemAddons)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_OrderItemAddons_OrderItems");
        });

        modelBuilder.Entity<OrderItemDiscount>(entity =>
        {
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DiscountName).HasMaxLength(150);
            entity.Property(e => e.DiscountType).HasMaxLength(20);

            entity.HasOne(d => d.Discount).WithMany(p => p.OrderItemDiscounts)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK_OrderItemDiscounts_Discounts");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemDiscounts)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_OrderItemDiscounts_OrderItems");
        });

        modelBuilder.Entity<OrderItemModifier>(entity =>
        {
            entity.Property(e => e.ModifierName).HasMaxLength(255);
            entity.Property(e => e.ModifierPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemModifiers)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_OrderItemModifiers_OrderItems");
        });

        modelBuilder.Entity<OrderItemTaxis>(entity =>
        {
            entity.HasKey(e => e.OrderItemTaxId);

            entity.Property(e => e.TaxAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TaxName).HasMaxLength(100);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TaxableAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemTaxes)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_OrderItemTaxes_OrderItems");

            entity.HasOne(d => d.Tax).WithMany(p => p.OrderItemTaxes)
                .HasForeignKey(d => d.TaxId)
                .HasConstraintName("FK_OrderItemTaxes_Taxs");
        });

        modelBuilder.Entity<OrderNotification>(entity =>
        {
            entity.Property(e => e.IsSent).HasDefaultValue(false);
            entity.Property(e => e.NotificationType).HasMaxLength(255);
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("PK__OrderSta__BC674CA1C9FA3EB6");

            entity.Property(e => e.OrderStatusId).ValueGeneratedNever();
            entity.Property(e => e.IsTerminal).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.ChangedByUsername).HasMaxLength(100);
            entity.Property(e => e.IsVisibleToCustomer).HasDefaultValue(false);
            entity.Property(e => e.NewStatus).HasMaxLength(255);
            entity.Property(e => e.OldStatus).HasMaxLength(255);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_OrderStatusHistory_Users");

            entity.HasOne(d => d.NewOrderStatus).WithMany(p => p.OrderStatusHistoryNewOrderStatuses)
                .HasForeignKey(d => d.NewOrderStatusId)
                .HasConstraintName("FK_OrderStatusHistory_OrderStatuses1");

            entity.HasOne(d => d.OldOrderStatus).WithMany(p => p.OrderStatusHistoryOldOrderStatuses)
                .HasForeignKey(d => d.OldOrderStatusId)
                .HasConstraintName("FK_OrderStatusHistory_OrderStatuses");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderStatusHistory_Orders");
        });

        modelBuilder.Entity<OrderStatusTransitionsRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderSta__3214EC0700D52D65");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RequiredRole).HasMaxLength(500);
        });

        modelBuilder.Entity<OrderTaxis>(entity =>
        {
            entity.HasKey(e => e.OrderTaxId);

            entity.Property(e => e.TaxAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TaxName).HasMaxLength(100);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.TaxableAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderTaxes)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderTaxes_Orders");

            entity.HasOne(d => d.Tax).WithMany(p => p.OrderTaxes)
                .HasForeignKey(d => d.TaxId)
                .HasConstraintName("FK_OrderTaxes_Taxs");
        });

        modelBuilder.Entity<OrderType>(entity =>
        {
            entity.HasKey(e => e.OrderTypeId).HasName("PK__OrderTyp__23AC266C444FBAA4");

            entity.Property(e => e.OrderTypeId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.PaymentGateway).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(30);
            entity.Property(e => e.TransactionRef).HasMaxLength(100);

            entity.HasOne(d => d.CardMachine).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CardMachineId)
                .HasConstraintName("FK_Payments_CardMachines");

            entity.HasOne(d => d.HandledByUser).WithMany(p => p.Payments)
                .HasForeignKey(d => d.HandledByUserId)
                .HasConstraintName("FK_Payments_Users");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__DC31C1D319947BA5");

            entity.Property(e => e.PaymentMethodId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.HasKey(e => e.PaymentStatusId).HasName("PK__PaymentS__34F8AC3FE4B51DF8");

            entity.Property(e => e.PaymentStatusId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PermissionName).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CDC14092C1");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(200);

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Products)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_Products_HeadOffice");
        });

        modelBuilder.Entity<ProductAddOn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductA__3214EC070FACBF7D");

            entity.ToTable("ProductAddOn");

            entity.HasIndex(e => new { e.ProductId, e.AddOnId }, "UQ_Product_AddOn").IsUnique();

            entity.HasOne(d => d.AddOn).WithMany(p => p.ProductAddOns)
                .HasForeignKey(d => d.AddOnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAddOn_AddOn");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.ProductAddOns)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_ProductAddOn_HeadOffice");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAddOns)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAddOn_Product");
        });

        modelBuilder.Entity<ProductModifier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductM__3214EC07F3CA40A7");

            entity.ToTable("ProductModifier");

            entity.HasIndex(e => new { e.ProductId, e.ModifierId }, "UQ_BranchProduct_Modifier").IsUnique();

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.ProductModifiers)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_ProductModifier_HeadOffice");

            entity.HasOne(d => d.Modifier).WithMany(p => p.ProductModifiers)
                .HasForeignKey(d => d.ModifierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductModifier_Modifier");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductModifiers)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductModifier_Product");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "IX_Roles_RoleName_UNIQUE").IsUnique();

            entity.Property(e => e.RoleDescription).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.ToTable("Table");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.TableLocation).HasMaxLength(255);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.TableNumber).HasMaxLength(50);
            entity.Property(e => e.TableStatus).HasMaxLength(50);

            entity.HasOne(d => d.Branch).WithMany(p => p.Tables)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_Table_Branches");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Tables)
                .HasForeignKey(d => d.HeadOfficeId)
                .HasConstraintName("FK_Table_HeadOffice");
        });

        modelBuilder.Entity<Tax>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsCompound).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PaymentType).HasMaxLength(70);
            entity.Property(e => e.Rate).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.HeadOffice).WithMany(p => p.Taxes)
                .HasForeignKey(d => d.HeadOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Taxs_HeadOffice");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_accounts.Users");

            entity.HasIndex(e => e.Email, "IX_Users_Email_UNIQUE").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "IX_Users_Phone_UNIQUE").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.PasswordSalt).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserBranch>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
