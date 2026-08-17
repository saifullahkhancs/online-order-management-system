/* =============================================================================
   OOMS - Online Order Management System
   SQL Server schema creation script
   -----------------------------------------------------------------------------
   Target      : Microsoft SQL Server 2019 or newer (also runs on Azure SQL /
                 SQL Server 2022 / SQL Server Express / mssql Docker image)
   Database    : OOMS
   Generated   : derived from Restaurant.Infrastructure/Persistence/AppDbContext.cs
                 and Restaurant.Infrastructure/Persistence/Models/*.cs
   -----------------------------------------------------------------------------
   HOW TO RUN

     sqlcmd -S localhost -U sa -P "Your_password123" -i ooms-schema.sql

   or open the file in SQL Server Management Studio / Azure Data Studio and press
   Execute. The script is idempotent: every object is created only when missing,
   so it is safe to re-run against an existing database.

   -----------------------------------------------------------------------------
   SECTIONS
     1. Database creation
     2. Tables            (53 tables, created in dependency order)
     3. Indexes           (unique + helper indexes)
     4. Foreign keys
     5. Lookup / reference seed data
        - OrderTypes, OrderStatuses, PaymentMethods, PaymentStatuses,
          DiscountAppliesTo, OrderStatusTransitionsRules, Roles
     6. Optional demo data (head office, branch, catalogue, menu)
        Guarded by @SeedDemoData - set it to 0 to skip.
   ============================================================================= */

SET NOCOUNT ON;
GO

/* ===========================================================================
   SECTION 1 - DATABASE
   =========================================================================== */

IF DB_ID(N'OOMS') IS NULL
BEGIN
    PRINT 'Creating database OOMS...';
    CREATE DATABASE [OOMS];
END
GO

ALTER DATABASE [OOMS] SET RECOVERY SIMPLE;
GO

USE [OOMS];
GO

/* ===========================================================================
   SECTION 2 - TABLES
   ---------------------------------------------------------------------------
   Tables are emitted in dependency (topological) order so that the foreign
   keys in section 4 always resolve. Column types, lengths, defaults, computed
   columns and primary key names mirror the EF Core model exactly, which means
   `Scaffold-DbContext` will regenerate the same entity classes that already
   live in Restaurant.Infrastructure/Persistence/Models.
   =========================================================================== */

-- ---------------------------------------------------------------------------
-- Table: dbo.HeadOffice
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.HeadOffice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[HeadOffice] (
        [HeadOfficeId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [Email] NVARCHAR(150) NULL,
        [Website] NVARCHAR(200) NULL,
        [AddressId] INT NULL,
        [BusinessCategory] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Restaurants] PRIMARY KEY CLUSTERED ([HeadOfficeId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.AddOnCategory
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.AddOnCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[AddOnCategory] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [MinSelect] INT NULL CONSTRAINT [DF_AddOnCategory_MinSelect] DEFAULT ((0)),
        [MaxSelect] INT NULL CONSTRAINT [DF_AddOnCategory_MaxSelect] DEFAULT ((5)),
        [IsActive] BIT NULL CONSTRAINT [DF_AddOnCategory_IsActive] DEFAULT ((1)),
        [SortOrder] INT NULL,
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__AddOnCat__3214EC078C95D93E] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.AddOn
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.AddOn', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[AddOn] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [AddOnCategoryId] INT NOT NULL,
        [AddOnUnitPrice] DECIMAL(10, 2) NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_AddOn_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NULL CONSTRAINT [DF_AddOn_IsDeleted] DEFAULT ((0)),
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__AddOn__3214EC07A6FA7E36] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Addresses
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Addresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Addresses] (
        [AddressId] INT IDENTITY(1,1) NOT NULL,
        [CustomerId] INT NULL,
        [AddressLine1] NVARCHAR(255) NOT NULL,
        [AddressLine2] NVARCHAR(255) NULL,
        [AddressLine3] NVARCHAR(255) NULL,
        [City] NVARCHAR(100) NOT NULL,
        [State] NVARCHAR(100) NULL,
        [Country] NVARCHAR(100) NOT NULL,
        [PostalCode] NVARCHAR(20) NULL,
        [IsDefault] BIT NULL CONSTRAINT [DF_Addresses_IsDefault] DEFAULT ((0)),
        CONSTRAINT [PK_Addresses] PRIMARY KEY CLUSTERED ([AddressId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Branches
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Branches] (
        [BranchId] INT IDENTITY(1,1) NOT NULL,
        [HeadOfficeId] INT NULL,
        [BranchName] NVARCHAR(200) NULL,
        [AddressId] INT NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [Email] NVARCHAR(150) NULL,
        [Latitude] DECIMAL(9, 6) NULL,
        [Longitude] DECIMAL(9, 6) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Branches_IsActive] DEFAULT ((1)),
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY CLUSTERED ([BranchId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Products
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Products] (
        [ProductId] INT IDENTITY(1,1) NOT NULL,
        [ProductName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Price] DECIMAL(18, 2) NOT NULL,
        [IsAvailable] BIT NOT NULL CONSTRAINT [DF_Products_IsAvailable] DEFAULT ((1)),
        [IsDeleted] BIT NOT NULL,
        [HeadOfficeId] INT NULL,
        [ImageUrl] NVARCHAR(500) NULL,
        CONSTRAINT [PK__Products__B40CC6CDC14092C1] PRIMARY KEY CLUSTERED ([ProductId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.BranchProduct
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BranchProduct', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[BranchProduct] (
        [BranchProductId] INT IDENTITY(1,1) NOT NULL,
        [BranchId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Price] DECIMAL(18, 2) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_BranchProduct_IsActive] DEFAULT ((1)),
        CONSTRAINT [PK__BranchPr__190F7B5BD79CE326] PRIMARY KEY CLUSTERED ([BranchProductId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.BranchTaxes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BranchTaxes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[BranchTaxes] (
        [BranchTaxId] INT IDENTITY(1,1) NOT NULL,
        [BranchId] INT NULL,
        [TaxId] INT NULL,
        [Priority] INT NULL,
        [IsCompound] BIT NULL,
        [IsActive] BIT NULL,
        CONSTRAINT [PK_BranchTaxes] PRIMARY KEY CLUSTERED ([BranchTaxId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.BranchTimings
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BranchTimings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[BranchTimings] (
        [BranchTimingId] INT IDENTITY(1,1) NOT NULL,
        [BranchId] INT NULL,
        [BranchTimingName] NVARCHAR(150) NULL,
        [OpenTime] TIME NULL,
        [CloseTime] TIME NULL,
        [IsClosed] BIT NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [SortOrder] INT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_BranchTimings_IsActive] DEFAULT ((1)),
        CONSTRAINT [PK_BranchTimings] PRIMARY KEY CLUSTERED ([BranchTimingId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.CardMachines
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CardMachines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[CardMachines] (
        [CardMachineId] INT NOT NULL,
        [MachineName] NVARCHAR(200) NULL,
        [MachineLocation] NVARCHAR(MAX) NULL,
        [MachineStatus] NVARCHAR(100) NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_CardMachines_IsActive] DEFAULT ((0)),
        CONSTRAINT [PK_CardMachines] PRIMARY KEY CLUSTERED ([CardMachineId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Carts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Carts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Carts] (
        [CartId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NULL,
        [GuestSessionToken] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Carts_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NOT NULL,
        [BranchId] INT NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY CLUSTERED ([CartId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.CartItems
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CartItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[CartItems] (
        [CartItemId] INT IDENTITY(1,1) NOT NULL,
        [CartId] INT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NULL,
        [UnitPrice] DECIMAL(10, 2) NULL,
        [TotalPrice] AS ([UnitPrice]*[Quantity]) PERSISTED,
        [CreatedAt] DATETIME2 NULL,
        [UpdatedAt] DATETIME2 NULL,
        [Instructions] NVARCHAR(500) NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY CLUSTERED ([CartItemId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.CartItemAddons
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CartItemAddons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[CartItemAddons] (
        [CartItemAddonId] INT IDENTITY(1,1) NOT NULL,
        [CartItemId] INT NULL,
        [AddOnId] INT NULL,
        [AddOnName] NVARCHAR(255) NULL,
        [AddOnPrice] DECIMAL(10, 2) NULL,
        [AddOnQuantity] INT NULL,
        [AddonCategoryId] INT NULL,
        CONSTRAINT [PK_CartItemAddons] PRIMARY KEY CLUSTERED ([CartItemAddonId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.CartItemModifiers
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CartItemModifiers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[CartItemModifiers] (
        [CartItemModifierId] INT IDENTITY(1,1) NOT NULL,
        [CartItemId] INT NULL,
        [ModifierCategoryId] INT NULL,
        [ModifierId] INT NULL,
        [ModifierName] NVARCHAR(255) NULL,
        [ModifierPrice] DECIMAL(10, 2) NULL,
        CONSTRAINT [PK_CartItemModifiers] PRIMARY KEY CLUSTERED ([CartItemModifierId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Categories
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Categories] (
        [CategoryId] INT IDENTITY(1,1) NOT NULL,
        [CategoryName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NOT NULL,
        [ImageUrl] NVARCHAR(500) NULL,
        [HeadOfficeId] INT NOT NULL,
        CONSTRAINT [PK__Categori__19093A0B2F35A8B0] PRIMARY KEY CLUSTERED ([CategoryId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.CategoryProduct
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CategoryProduct', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[CategoryProduct] (
        [CategoryProductId] INT IDENTITY(1,1) NOT NULL,
        [CategoryId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_CategoryProduct_IsActive] DEFAULT ((1)),
        CONSTRAINT [PK__Category__FAFA184FEEC5AAC3] PRIMARY KEY CLUSTERED ([CategoryProductId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Customers
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Customers] (
        [CustomerId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NULL,
        [FullName] NVARCHAR(150) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [Email] NVARCHAR(255) NULL,
        [LoyaltyNumber] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([CustomerId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Discounts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Discounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Discounts] (
        [DiscountId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [DiscountType] INT NOT NULL,
        [Value] DECIMAL(10, 2) NOT NULL,
        [AppliesTo] INT NOT NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Discounts_IsActive] DEFAULT ((1)),
        [MinimumOrderAmount] DECIMAL(10, 2) NULL,
        [MaximumDiscountAmount] DECIMAL(10, 2) NULL,
        [CreatedBy] INT NULL,
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Discounts_CreatedDate] DEFAULT (getdate()),
        [UpdatedBy] INT NULL,
        [UpdatedDate] DATETIME2 NULL,
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__Discount__E43F6D9646621B2A] PRIMARY KEY CLUSTERED ([DiscountId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountAppliesTo
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountAppliesTo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountAppliesTo] (
        [DiscountAppliesToId] INT NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK__Discount__1CD7B727F6167247] PRIMARY KEY CLUSTERED ([DiscountAppliesToId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountBranches
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountBranches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountBranches] (
        [DiscountBranchId] INT IDENTITY(1,1) NOT NULL,
        [DiscountId] INT NOT NULL,
        [BranchId] INT NOT NULL,
        CONSTRAINT [PK__Discount__FCB0183851209747] PRIMARY KEY CLUSTERED ([DiscountBranchId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountCategories
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountCategories] (
        [DiscountCategoryId] INT IDENTITY(1,1) NOT NULL,
        [DiscountId] INT NOT NULL,
        [CategoryId] INT NOT NULL,
        CONSTRAINT [PK__Discount__25BD2140C2F1D61C] PRIMARY KEY CLUSTERED ([DiscountCategoryId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountCoupons
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountCoupons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountCoupons] (
        [CouponId] INT IDENTITY(1,1) NOT NULL,
        [DiscountId] INT NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [UsageLimit] INT NULL,
        [PerUserLimit] INT NULL,
        [IsPublic] BIT NOT NULL CONSTRAINT [DF_DiscountCoupons_IsPublic] DEFAULT ((1)),
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_DiscountCoupons_CreatedDate] DEFAULT (getdate()),
        CONSTRAINT [PK__Discount__384AF1BA6378C01B] PRIMARY KEY CLUSTERED ([CouponId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountProducts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountProducts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountProducts] (
        [DiscountProductId] INT IDENTITY(1,1) NOT NULL,
        [DiscountId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        CONSTRAINT [PK__Discount__0B7AC54D6965C34D] PRIMARY KEY CLUSTERED ([DiscountProductId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Table
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Table', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Table] (
        [TableId] INT IDENTITY(1,1) NOT NULL,
        [TableName] NVARCHAR(100) NULL,
        [TableNumber] NVARCHAR(50) NULL,
        [TableLocation] NVARCHAR(255) NULL,
        [TableCapacity] INT NULL,
        [TableStatus] NVARCHAR(50) NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_Table_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NULL CONSTRAINT [DF_Table_IsDeleted] DEFAULT ((0)),
        [HeadOfficeId] INT NULL,
        [BranchId] INT NULL,
        CONSTRAINT [PK_Table] PRIMARY KEY CLUSTERED ([TableId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Orders
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Orders] (
        [OrderId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderNumber] NVARCHAR(100) NULL,
        [BranchId] INT NULL,
        [CustomerId] BIGINT NULL,
        [OrderTypeId] INT NULL,
        [OrderDate] DATETIME2 NULL,
        [Subtotal] DECIMAL(10, 2) NULL,
        [TaxAmount] DECIMAL(10, 2) NULL,
        [DiscountAmount] DECIMAL(10, 2) NULL,
        [DeliveryFee] DECIMAL(10, 2) NULL,
        [TotalAmount] DECIMAL(10, 2) NULL,
        [PaymentId] BIGINT NULL,
        [PaymentStatus] NVARCHAR(255) NULL,
        [OrderStatusId] INT NULL,
        [OrderStatus] NVARCHAR(255) NULL,
        [DeliveryAddressId] BIGINT NULL,
        [DeliveryAddress] NVARCHAR(MAX) NULL,
        [DeliveryInstructions] NVARCHAR(500) NULL,
        [GuestSessionToken] UNIQUEIDENTIFIER NULL,
        [GuestName] NVARCHAR(255) NULL,
        [GuestPhoneNumber] NVARCHAR(50) NULL,
        [GuestEmailAddress] NVARCHAR(255) NULL,
        [GuestAddressLine1] NVARCHAR(500) NULL,
        [GuestAddressLine2] NVARCHAR(500) NULL,
        [GuestAddressLine3] NVARCHAR(500) NULL,
        [GuestCity] NVARCHAR(100) NULL,
        [GuestPostalCode] NVARCHAR(20) NULL,
        [GuestCountry] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_Orders_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NULL CONSTRAINT [DF_Orders_IsDeleted] DEFAULT ((0)),
        [TableId] INT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.DiscountUsage
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DiscountUsage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiscountUsage] (
        [DiscountUsageId] INT IDENTITY(1,1) NOT NULL,
        [DiscountId] INT NOT NULL,
        [CouponId] INT NULL,
        [UserId] INT NULL,
        [OrderId] BIGINT NOT NULL,
        [UsedDate] DATETIME2 NOT NULL CONSTRAINT [DF_DiscountUsage_UsedDate] DEFAULT (getdate()),
        CONSTRAINT [PK__Discount__C5D41D63D315E6AF] PRIMARY KEY CLUSTERED ([DiscountUsageId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Feedbacks
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Feedbacks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Feedbacks] (
        [FeedbackId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [CustomerId] INT NULL,
        [GuestSessionToken] UNIQUEIDENTIFIER NULL,
        [Rating] INT NULL,
        [Comments] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NULL,
        [BranchId] INT NULL,
        [FeedbackSource] NVARCHAR(100) NULL,
        [IsResolved] BIT NULL,
        [ResolvedBy] INT NULL,
        [ResolutionNote] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_Feedbacks] PRIMARY KEY CLUSTERED ([FeedbackId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.ModifierCategory
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ModifierCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ModifierCategory] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Price] DECIMAL(18, 2) NOT NULL,
        [IsRequired] BIT NULL CONSTRAINT [DF_ModifierCategory_IsRequired] DEFAULT ((0)),
        [IsActive] BIT NULL CONSTRAINT [DF_ModifierCategory_IsActive] DEFAULT ((1)),
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__Modifier__3214EC076D5A0DE6] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Modifier
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Modifier', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Modifier] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [CategoryId] INT NOT NULL,
        [DefaultPrice] DECIMAL(18, 2) NOT NULL,
        [HeadOfficeId] INT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Modifier_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK__Modifier__3214EC07DF291039] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderDiscounts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderDiscounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderDiscounts] (
        [OrderDiscountId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [DiscountId] INT NULL,
        [DiscountCode] NVARCHAR(50) NULL,
        [DiscountName] NVARCHAR(150) NULL,
        [DiscountAmount] DECIMAL(10, 2) NULL,
        [DiscountType] NVARCHAR(20) NULL,
        CONSTRAINT [PK_OrderDiscounts] PRIMARY KEY CLUSTERED ([OrderDiscountId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderItems
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderItems] (
        [OrderItemId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [ProductId] INT NULL,
        [ProductName] NVARCHAR(200) NULL,
        [Quantity] INT NULL,
        [UnitPrice] DECIMAL(10, 2) NULL,
        [FinalPrice] DECIMAL(10, 2) NULL,
        [Instructions] NVARCHAR(500) NULL,
        [DiscountAmount] DECIMAL(10, 2) NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([OrderItemId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderItemAddons
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderItemAddons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderItemAddons] (
        [OrderItemAddonId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderItemId] BIGINT NULL,
        [AddonId] INT NULL,
        [AddonName] NVARCHAR(255) NULL,
        [AddonQuantity] INT NULL,
        [AddonPrice] DECIMAL(10, 2) NULL,
        [AddonCategoryId] INT NULL,
        CONSTRAINT [PK_OrderItemAddons] PRIMARY KEY CLUSTERED ([OrderItemAddonId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderItemDiscounts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderItemDiscounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderItemDiscounts] (
        [OrderItemDiscountId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderItemId] BIGINT NULL,
        [DiscountId] INT NULL,
        [DiscountName] NVARCHAR(150) NULL,
        [DiscountAmount] DECIMAL(10, 2) NULL,
        [DiscountType] NVARCHAR(20) NULL,
        CONSTRAINT [PK_OrderItemDiscounts] PRIMARY KEY CLUSTERED ([OrderItemDiscountId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderItemModifiers
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderItemModifiers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderItemModifiers] (
        [OrderItemModifierId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderItemId] BIGINT NULL,
        [ModifierId] INT NULL,
        [ModifierName] NVARCHAR(255) NULL,
        [ModifierPrice] DECIMAL(10, 2) NULL,
        CONSTRAINT [PK_OrderItemModifiers] PRIMARY KEY CLUSTERED ([OrderItemModifierId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Taxs
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Taxs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Taxs] (
        [TaxId] INT IDENTITY(1,1) NOT NULL,
        [Code] NVARCHAR(50) NULL,
        [Name] NVARCHAR(100) NULL,
        [Rate] DECIMAL(10, 2) NULL,
        [IsPercentage] BIT NULL,
        [IsCompound] BIT NULL CONSTRAINT [DF_Taxs_IsCompound] DEFAULT ((0)),
        [Priority] INT NULL,
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_Taxs_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NULL CONSTRAINT [DF_Taxs_IsDeleted] DEFAULT ((0)),
        [PaymentTypeId] INT NULL,
        [PaymentType] NVARCHAR(70) NULL,
        [HeadOfficeId] INT NOT NULL,
        CONSTRAINT [PK_Taxs] PRIMARY KEY CLUSTERED ([TaxId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderItemTaxes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderItemTaxes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderItemTaxes] (
        [OrderItemTaxId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderItemId] BIGINT NULL,
        [TaxId] INT NULL,
        [TaxName] NVARCHAR(100) NULL,
        [TaxRate] DECIMAL(10, 2) NULL,
        [TaxableAmount] DECIMAL(10, 2) NULL,
        [TaxAmount] DECIMAL(10, 2) NULL,
        CONSTRAINT [PK_OrderItemTaxes] PRIMARY KEY CLUSTERED ([OrderItemTaxId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderNotifications
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderNotifications] (
        [OrderNotificationId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [NotificationTypeId] INT NULL,
        [NotificationType] NVARCHAR(255) NULL,
        [NotificationPriority] INT NULL,
        [MessageText] NVARCHAR(MAX) NULL,
        [IsSent] BIT NULL CONSTRAINT [DF_OrderNotifications_IsSent] DEFAULT ((0)),
        [SentAt] DATETIME2 NULL,
        CONSTRAINT [PK_OrderNotifications] PRIMARY KEY CLUSTERED ([OrderNotificationId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderStatuses
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderStatuses] (
        [OrderStatusId] INT NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        [IsTerminal] BIT NULL CONSTRAINT [DF_OrderStatuses_IsTerminal] DEFAULT ((0)),
        CONSTRAINT [PK__OrderSta__BC674CA1C9FA3EB6] PRIMARY KEY CLUSTERED ([OrderStatusId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Users
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Users] (
        [UserId] INT IDENTITY(1,1) NOT NULL,
        [Username] NVARCHAR(100) NULL,
        [Email] NVARCHAR(255) NULL,
        [PasswordHash] VARBINARY(512) NULL,
        [PasswordSalt] VARBINARY(256) NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [CreatedBy] INT NULL,
        [CreatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [IsActive] BIT NULL,
        [IsDeleted] BIT NULL,
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK_accounts.Users] PRIMARY KEY CLUSTERED ([UserId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderStatusHistory
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderStatusHistory] (
        [OrderStatusHistoryId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [OldOrderStatusId] INT NULL,
        [NewOrderStatusId] INT NULL,
        [OldStatus] NVARCHAR(255) NULL,
        [NewStatus] NVARCHAR(255) NULL,
        [ChangedAt] DATETIME2 NULL,
        [ChangedByUsername] NVARCHAR(100) NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [ChangedByUserId] INT NULL,
        [IsVisibleToCustomer] BIT NULL CONSTRAINT [DF_OrderStatusHistory_IsVisibleToCustomer] DEFAULT ((0)),
        CONSTRAINT [PK_OrderStatusHistory] PRIMARY KEY CLUSTERED ([OrderStatusHistoryId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderStatusTransitionsRules
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderStatusTransitionsRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderStatusTransitionsRules] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderTypeId] INT NOT NULL,
        [FromStatusId] INT NOT NULL,
        [ToStatusId] INT NOT NULL,
        [RequiredRole] NVARCHAR(500) NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_OrderStatusTransitionsRules_IsActive] DEFAULT ((1)),
        [CreatedAt] DATETIME NULL CONSTRAINT [DF_OrderStatusTransitionsRules_CreatedAt] DEFAULT (getdate()),
        CONSTRAINT [PK__OrderSta__3214EC0700D52D65] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderTaxes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderTaxes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderTaxes] (
        [OrderTaxId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [TaxId] INT NULL,
        [TaxName] NVARCHAR(100) NULL,
        [TaxRate] DECIMAL(10, 4) NULL,
        [TaxableAmount] DECIMAL(10, 2) NULL,
        [TaxAmount] DECIMAL(10, 2) NULL,
        CONSTRAINT [PK_OrderTaxes] PRIMARY KEY CLUSTERED ([OrderTaxId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.OrderTypes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrderTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[OrderTypes] (
        [OrderTypeId] INT NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK__OrderTyp__23AC266C444FBAA4] PRIMARY KEY CLUSTERED ([OrderTypeId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Payments
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Payments] (
        [PaymentId] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NULL,
        [PaymentMethod] NVARCHAR(50) NULL,
        [AmountPaid] DECIMAL(10, 0) NULL,
        [TransactionRef] NVARCHAR(100) NULL,
        [PaymentGateway] NVARCHAR(50) NULL,
        [PaymentStatus] NVARCHAR(30) NULL,
        [PaymentDate] DATETIME2 NULL,
        [GatewayResponse] NVARCHAR(MAX) NULL,
        [CardMachineId] INT NULL,
        [HandledByUserId] INT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([PaymentId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.PaymentMethods
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.PaymentMethods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[PaymentMethods] (
        [PaymentMethodId] INT NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK__PaymentM__DC31C1D319947BA5] PRIMARY KEY CLUSTERED ([PaymentMethodId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.PaymentStatuses
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.PaymentStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[PaymentStatuses] (
        [PaymentStatusId] INT NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK__PaymentS__34F8AC3FE4B51DF8] PRIMARY KEY CLUSTERED ([PaymentStatusId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Permissions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Permissions] (
        [PermissionId] INT IDENTITY(1,1) NOT NULL,
        [PermissionName] NVARCHAR(100) NULL,
        [Description] NVARCHAR(255) NULL,
        [IsActive] BIT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.ProductAddOn
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProductAddOn', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProductAddOn] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductId] INT NOT NULL,
        [AddOnId] INT NOT NULL,
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__ProductA__3214EC070FACBF7D] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.ProductModifier
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ProductModifier', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProductModifier] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductId] INT NOT NULL,
        [ModifierId] INT NOT NULL,
        [HeadOfficeId] INT NULL,
        CONSTRAINT [PK__ProductM__3214EC07F3CA40A7] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.Roles
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Roles] (
        [RoleId] INT IDENTITY(1,1) NOT NULL,
        [RoleName] NVARCHAR(50) NULL,
        [RoleDescription] NVARCHAR(255) NULL,
        [IsActive] BIT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.RolePermissions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[RolePermissions] (
        [RolePermissionId] INT IDENTITY(1,1) NOT NULL,
        [RoleId] INT NOT NULL,
        [PermissionId] INT NOT NULL,
        [IsActive] BIT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.UserBranches
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.UserBranches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[UserBranches] (
        [UserBranchId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [BranchId] INT NOT NULL,
        [RoleId] INT NULL,
        [IsActive] BIT NULL CONSTRAINT [DF_UserBranches_IsActive] DEFAULT ((1)),
        [IsDeleted] BIT NULL CONSTRAINT [DF_UserBranches_IsDeleted] DEFAULT ((0)),
        [CreatedAt] DATETIME2 NULL,
        [CreatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] INT NULL,
        CONSTRAINT [PK_UserBranches] PRIMARY KEY CLUSTERED ([UserBranchId] ASC)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.UserRoles
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[UserRoles] (
        [UserRoleId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [IsActive] BIT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC)
    );
END
GO


/* ===========================================================================
   SECTION 3 - INDEXES
   =========================================================================== */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BranchProduct_Branch_Product' AND object_id = OBJECT_ID(N'dbo.BranchProduct'))
    CREATE UNIQUE INDEX [UQ_BranchProduct_Branch_Product] ON dbo.[BranchProduct] ([BranchId] ASC, [ProductId] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Carts_UserId_GuestSessionToken' AND object_id = OBJECT_ID(N'dbo.Carts'))
    CREATE UNIQUE INDEX [UQ_Carts_UserId_GuestSessionToken] ON dbo.[Carts] ([UserId] ASC, [GuestSessionToken] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems' AND object_id = OBJECT_ID(N'dbo.CartItems'))
    CREATE INDEX [IX_CartItems] ON dbo.[CartItems] ([CartItemId] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Category_Product' AND object_id = OBJECT_ID(N'dbo.CategoryProduct'))
    CREATE UNIQUE INDEX [UQ_Category_Product] ON dbo.[CategoryProduct] ([CategoryId] ASC, [ProductId] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ__Discount__A25C5AA7264F4835' AND object_id = OBJECT_ID(N'dbo.DiscountCoupons'))
    CREATE UNIQUE INDEX [UQ__Discount__A25C5AA7264F4835] ON dbo.[DiscountCoupons] ([Code] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email_UNIQUE' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX [IX_Users_Email_UNIQUE] ON dbo.[Users] ([Email] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Phone_UNIQUE' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX [IX_Users_Phone_UNIQUE] ON dbo.[Users] ([PhoneNumber] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Product_AddOn' AND object_id = OBJECT_ID(N'dbo.ProductAddOn'))
    CREATE UNIQUE INDEX [UQ_Product_AddOn] ON dbo.[ProductAddOn] ([ProductId] ASC, [AddOnId] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BranchProduct_Modifier' AND object_id = OBJECT_ID(N'dbo.ProductModifier'))
    CREATE UNIQUE INDEX [UQ_BranchProduct_Modifier] ON dbo.[ProductModifier] ([ProductId] ASC, [ModifierId] ASC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Roles_RoleName_UNIQUE' AND object_id = OBJECT_ID(N'dbo.Roles'))
    CREATE UNIQUE INDEX [IX_Roles_RoleName_UNIQUE] ON dbo.[Roles] ([RoleName] ASC);
GO

/* ===========================================================================
   SECTION 4 - FOREIGN KEYS
   =========================================================================== */

IF OBJECT_ID(N'dbo.[FK_AddOnCategory_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[AddOnCategory] WITH CHECK ADD CONSTRAINT [FK_AddOnCategory_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_AddOn_AddOnCategory]', N'F') IS NULL
    ALTER TABLE dbo.[AddOn] WITH CHECK ADD CONSTRAINT [FK_AddOn_AddOnCategory]
        FOREIGN KEY ([AddOnCategoryId]) REFERENCES dbo.[AddOnCategory] ([Id]);
GO
IF OBJECT_ID(N'dbo.[FK_AddOn_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[AddOn] WITH CHECK ADD CONSTRAINT [FK_AddOn_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_Branches_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Branches] WITH CHECK ADD CONSTRAINT [FK_Branches_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_Products_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Products] WITH CHECK ADD CONSTRAINT [FK_Products_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_BranchProduct_Branches]', N'F') IS NULL
    ALTER TABLE dbo.[BranchProduct] WITH CHECK ADD CONSTRAINT [FK_BranchProduct_Branches]
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branches] ([BranchId]);
GO
IF OBJECT_ID(N'dbo.[FK_BranchProduct_Products]', N'F') IS NULL
    ALTER TABLE dbo.[BranchProduct] WITH CHECK ADD CONSTRAINT [FK_BranchProduct_Products]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO
IF OBJECT_ID(N'dbo.[FK_BranchTimings_Branches]', N'F') IS NULL
    ALTER TABLE dbo.[BranchTimings] WITH CHECK ADD CONSTRAINT [FK_BranchTimings_Branches]
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branches] ([BranchId]);
GO
IF OBJECT_ID(N'dbo.[FK_CartItems_Cart]', N'F') IS NULL
    ALTER TABLE dbo.[CartItems] WITH CHECK ADD CONSTRAINT [FK_CartItems_Cart]
        FOREIGN KEY ([CartId]) REFERENCES dbo.[Carts] ([CartId]);
GO
IF OBJECT_ID(N'dbo.[FK_CartItemAddons_CartItems]', N'F') IS NULL
    ALTER TABLE dbo.[CartItemAddons] WITH CHECK ADD CONSTRAINT [FK_CartItemAddons_CartItems]
        FOREIGN KEY ([CartItemId]) REFERENCES dbo.[CartItems] ([CartItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_CartItemModifiers_CartItems]', N'F') IS NULL
    ALTER TABLE dbo.[CartItemModifiers] WITH CHECK ADD CONSTRAINT [FK_CartItemModifiers_CartItems]
        FOREIGN KEY ([CartItemId]) REFERENCES dbo.[CartItems] ([CartItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_Categories_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Categories] WITH CHECK ADD CONSTRAINT [FK_Categories_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_CategoryProduct_Category]', N'F') IS NULL
    ALTER TABLE dbo.[CategoryProduct] WITH CHECK ADD CONSTRAINT [FK_CategoryProduct_Category]
        FOREIGN KEY ([CategoryId]) REFERENCES dbo.[Categories] ([CategoryId]);
GO
IF OBJECT_ID(N'dbo.[FK_CategoryProduct_Product]', N'F') IS NULL
    ALTER TABLE dbo.[CategoryProduct] WITH CHECK ADD CONSTRAINT [FK_CategoryProduct_Product]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO
IF OBJECT_ID(N'dbo.[FK_Discounts_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Discounts] WITH CHECK ADD CONSTRAINT [FK_Discounts_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountB__Branc__5A846E65]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountBranches] WITH CHECK ADD CONSTRAINT [FK__DiscountB__Branc__5A846E65]
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branches] ([BranchId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountB__Disco__59904A2C]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountBranches] WITH CHECK ADD CONSTRAINT [FK__DiscountB__Disco__59904A2C]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountC__Categ__6225902D]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountCategories] WITH CHECK ADD CONSTRAINT [FK__DiscountC__Categ__6225902D]
        FOREIGN KEY ([CategoryId]) REFERENCES dbo.[Categories] ([CategoryId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountC__Disco__61316BF4]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountCategories] WITH CHECK ADD CONSTRAINT [FK__DiscountC__Disco__61316BF4]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountC__Disco__67DE6983]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountCoupons] WITH CHECK ADD CONSTRAINT [FK__DiscountC__Disco__67DE6983]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountP__Disco__5D60DB10]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountProducts] WITH CHECK ADD CONSTRAINT [FK__DiscountP__Disco__5D60DB10]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountP__Produ__5E54FF49]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountProducts] WITH CHECK ADD CONSTRAINT [FK__DiscountP__Produ__5E54FF49]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO
IF OBJECT_ID(N'dbo.[FK_Table_Branches]', N'F') IS NULL
    ALTER TABLE dbo.[Table] WITH CHECK ADD CONSTRAINT [FK_Table_Branches]
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branches] ([BranchId]);
GO
IF OBJECT_ID(N'dbo.[FK_Table_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Table] WITH CHECK ADD CONSTRAINT [FK_Table_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_Orders_Table]', N'F') IS NULL
    ALTER TABLE dbo.[Orders] WITH CHECK ADD CONSTRAINT [FK_Orders_Table]
        FOREIGN KEY ([TableId]) REFERENCES dbo.[Table] ([TableId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountU__Coupo__6CA31EA0]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountUsage] WITH CHECK ADD CONSTRAINT [FK__DiscountU__Coupo__6CA31EA0]
        FOREIGN KEY ([CouponId]) REFERENCES dbo.[DiscountCoupons] ([CouponId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountU__Disco__6BAEFA67]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountUsage] WITH CHECK ADD CONSTRAINT [FK__DiscountU__Disco__6BAEFA67]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK__DiscountU__Order__6D9742D9]', N'F') IS NULL
    ALTER TABLE dbo.[DiscountUsage] WITH CHECK ADD CONSTRAINT [FK__DiscountU__Order__6D9742D9]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_Feedbacks_Branches]', N'F') IS NULL
    ALTER TABLE dbo.[Feedbacks] WITH CHECK ADD CONSTRAINT [FK_Feedbacks_Branches]
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branches] ([BranchId]);
GO
IF OBJECT_ID(N'dbo.[FK_Feedbacks_Customers]', N'F') IS NULL
    ALTER TABLE dbo.[Feedbacks] WITH CHECK ADD CONSTRAINT [FK_Feedbacks_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES dbo.[Customers] ([CustomerId]);
GO
IF OBJECT_ID(N'dbo.[FK_Feedbacks_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[Feedbacks] WITH CHECK ADD CONSTRAINT [FK_Feedbacks_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_ModifierCategory_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[ModifierCategory] WITH CHECK ADD CONSTRAINT [FK_ModifierCategory_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_Modifier_ModifierCategory]', N'F') IS NULL
    ALTER TABLE dbo.[Modifier] WITH CHECK ADD CONSTRAINT [FK_Modifier_ModifierCategory]
        FOREIGN KEY ([CategoryId]) REFERENCES dbo.[ModifierCategory] ([Id]);
GO
IF OBJECT_ID(N'dbo.[FK_Modifier_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Modifier] WITH CHECK ADD CONSTRAINT [FK_Modifier_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderDiscounts_Discounts]', N'F') IS NULL
    ALTER TABLE dbo.[OrderDiscounts] WITH CHECK ADD CONSTRAINT [FK_OrderDiscounts_Discounts]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderDiscounts_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[OrderDiscounts] WITH CHECK ADD CONSTRAINT [FK_OrderDiscounts_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItems_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItems] WITH CHECK ADD CONSTRAINT [FK_OrderItems_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItems_Products]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItems] WITH CHECK ADD CONSTRAINT [FK_OrderItems_Products]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemAddons_OrderItems]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemAddons] WITH CHECK ADD CONSTRAINT [FK_OrderItemAddons_OrderItems]
        FOREIGN KEY ([OrderItemId]) REFERENCES dbo.[OrderItems] ([OrderItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemDiscounts_Discounts]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemDiscounts] WITH CHECK ADD CONSTRAINT [FK_OrderItemDiscounts_Discounts]
        FOREIGN KEY ([DiscountId]) REFERENCES dbo.[Discounts] ([DiscountId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemDiscounts_OrderItems]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemDiscounts] WITH CHECK ADD CONSTRAINT [FK_OrderItemDiscounts_OrderItems]
        FOREIGN KEY ([OrderItemId]) REFERENCES dbo.[OrderItems] ([OrderItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemModifiers_OrderItems]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemModifiers] WITH CHECK ADD CONSTRAINT [FK_OrderItemModifiers_OrderItems]
        FOREIGN KEY ([OrderItemId]) REFERENCES dbo.[OrderItems] ([OrderItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_Taxs_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[Taxs] WITH CHECK ADD CONSTRAINT [FK_Taxs_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemTaxes_OrderItems]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemTaxes] WITH CHECK ADD CONSTRAINT [FK_OrderItemTaxes_OrderItems]
        FOREIGN KEY ([OrderItemId]) REFERENCES dbo.[OrderItems] ([OrderItemId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderItemTaxes_Taxs]', N'F') IS NULL
    ALTER TABLE dbo.[OrderItemTaxes] WITH CHECK ADD CONSTRAINT [FK_OrderItemTaxes_Taxs]
        FOREIGN KEY ([TaxId]) REFERENCES dbo.[Taxs] ([TaxId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderStatusHistory_Users]', N'F') IS NULL
    ALTER TABLE dbo.[OrderStatusHistory] WITH CHECK ADD CONSTRAINT [FK_OrderStatusHistory_Users]
        FOREIGN KEY ([ChangedByUserId]) REFERENCES dbo.[Users] ([UserId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderStatusHistory_OrderStatuses1]', N'F') IS NULL
    ALTER TABLE dbo.[OrderStatusHistory] WITH CHECK ADD CONSTRAINT [FK_OrderStatusHistory_OrderStatuses1]
        FOREIGN KEY ([NewOrderStatusId]) REFERENCES dbo.[OrderStatuses] ([OrderStatusId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderStatusHistory_OrderStatuses]', N'F') IS NULL
    ALTER TABLE dbo.[OrderStatusHistory] WITH CHECK ADD CONSTRAINT [FK_OrderStatusHistory_OrderStatuses]
        FOREIGN KEY ([OldOrderStatusId]) REFERENCES dbo.[OrderStatuses] ([OrderStatusId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderStatusHistory_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[OrderStatusHistory] WITH CHECK ADD CONSTRAINT [FK_OrderStatusHistory_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderTaxes_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[OrderTaxes] WITH CHECK ADD CONSTRAINT [FK_OrderTaxes_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_OrderTaxes_Taxs]', N'F') IS NULL
    ALTER TABLE dbo.[OrderTaxes] WITH CHECK ADD CONSTRAINT [FK_OrderTaxes_Taxs]
        FOREIGN KEY ([TaxId]) REFERENCES dbo.[Taxs] ([TaxId]);
GO
IF OBJECT_ID(N'dbo.[FK_Payments_CardMachines]', N'F') IS NULL
    ALTER TABLE dbo.[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_CardMachines]
        FOREIGN KEY ([CardMachineId]) REFERENCES dbo.[CardMachines] ([CardMachineId]);
GO
IF OBJECT_ID(N'dbo.[FK_Payments_Users]', N'F') IS NULL
    ALTER TABLE dbo.[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_Users]
        FOREIGN KEY ([HandledByUserId]) REFERENCES dbo.[Users] ([UserId]);
GO
IF OBJECT_ID(N'dbo.[FK_Payments_Orders]', N'F') IS NULL
    ALTER TABLE dbo.[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_Orders]
        FOREIGN KEY ([OrderId]) REFERENCES dbo.[Orders] ([OrderId]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductAddOn_AddOn]', N'F') IS NULL
    ALTER TABLE dbo.[ProductAddOn] WITH CHECK ADD CONSTRAINT [FK_ProductAddOn_AddOn]
        FOREIGN KEY ([AddOnId]) REFERENCES dbo.[AddOn] ([Id]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductAddOn_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[ProductAddOn] WITH CHECK ADD CONSTRAINT [FK_ProductAddOn_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductAddOn_Product]', N'F') IS NULL
    ALTER TABLE dbo.[ProductAddOn] WITH CHECK ADD CONSTRAINT [FK_ProductAddOn_Product]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductModifier_HeadOffice]', N'F') IS NULL
    ALTER TABLE dbo.[ProductModifier] WITH CHECK ADD CONSTRAINT [FK_ProductModifier_HeadOffice]
        FOREIGN KEY ([HeadOfficeId]) REFERENCES dbo.[HeadOffice] ([HeadOfficeId]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductModifier_Modifier]', N'F') IS NULL
    ALTER TABLE dbo.[ProductModifier] WITH CHECK ADD CONSTRAINT [FK_ProductModifier_Modifier]
        FOREIGN KEY ([ModifierId]) REFERENCES dbo.[Modifier] ([Id]);
GO
IF OBJECT_ID(N'dbo.[FK_ProductModifier_Product]', N'F') IS NULL
    ALTER TABLE dbo.[ProductModifier] WITH CHECK ADD CONSTRAINT [FK_ProductModifier_Product]
        FOREIGN KEY ([ProductId]) REFERENCES dbo.[Products] ([ProductId]);
GO

/* ===========================================================================
   SECTION 5 - LOOKUP / REFERENCE DATA
   ---------------------------------------------------------------------------
   These values are NOT optional. The application code maps them to the C#
   enums in Restaurant.Application/Enums/CheckoutEnums.cs, so the ids below
   must match exactly.
   =========================================================================== */

-------------------------------------------------------------------------------
-- 5.1  OrderTypes  (Restaurant.Application.Enums.OrderType)
-------------------------------------------------------------------------------
MERGE dbo.[OrderTypes] AS target
USING (VALUES
    (1, N'Delivery'),
    (2, N'Pickup'),
    (3, N'DineIn'),
    (4, N'Takeaway')
) AS source ([OrderTypeId], [Name])
    ON target.[OrderTypeId] = source.[OrderTypeId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([OrderTypeId], [Name]) VALUES (source.[OrderTypeId], source.[Name])
WHEN MATCHED THEN
    UPDATE SET target.[Name] = source.[Name];
GO

-------------------------------------------------------------------------------
-- 5.2  OrderStatuses  (Restaurant.Application.Enums.OrderStatus)
--
--      Delivery flow : Pending -> Confirmed -> Preparing -> OutForDelivery -> Delivered
--      Pickup flow   : Pending -> Confirmed -> Preparing -> ReadyForPickup -> Completed
-------------------------------------------------------------------------------
MERGE dbo.[OrderStatuses] AS target
USING (VALUES
    ( 1, N'Pending',        CAST(0 AS BIT)),
    ( 2, N'Confirmed',      CAST(0 AS BIT)),
    ( 3, N'Preparing',      CAST(0 AS BIT)),
    ( 4, N'ReadyForPickup', CAST(0 AS BIT)),
    ( 5, N'OutForDelivery', CAST(0 AS BIT)),
    ( 6, N'Delivered',      CAST(1 AS BIT)),
    ( 7, N'Completed',      CAST(1 AS BIT)),
    ( 8, N'Cancelled',      CAST(1 AS BIT)),
    ( 9, N'Rejected',       CAST(1 AS BIT)),
    (10, N'Refunded',       CAST(1 AS BIT))
) AS source ([OrderStatusId], [Name], [IsTerminal])
    ON target.[OrderStatusId] = source.[OrderStatusId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([OrderStatusId], [Name], [IsTerminal])
    VALUES (source.[OrderStatusId], source.[Name], source.[IsTerminal])
WHEN MATCHED THEN
    UPDATE SET target.[Name] = source.[Name], target.[IsTerminal] = source.[IsTerminal];
GO

-------------------------------------------------------------------------------
-- 5.3  PaymentMethods  (Restaurant.Application.Enums.PaymentMethod)
-------------------------------------------------------------------------------
MERGE dbo.[PaymentMethods] AS target
USING (VALUES
    (1, N'Cash'),
    (2, N'Card'),
    (3, N'Online'),
    (4, N'Wallet'),
    (5, N'Split')
) AS source ([PaymentMethodId], [Name])
    ON target.[PaymentMethodId] = source.[PaymentMethodId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([PaymentMethodId], [Name]) VALUES (source.[PaymentMethodId], source.[Name])
WHEN MATCHED THEN
    UPDATE SET target.[Name] = source.[Name];
GO

-------------------------------------------------------------------------------
-- 5.4  PaymentStatuses  (Restaurant.Application.Enums.PaymentStatus)
-------------------------------------------------------------------------------
MERGE dbo.[PaymentStatuses] AS target
USING (VALUES
    (1, N'Pending'),
    (2, N'Authorized'),
    (3, N'Paid'),
    (4, N'Failed'),
    (5, N'Refunded'),
    (6, N'Cancelled')
) AS source ([PaymentStatusId], [Name])
    ON target.[PaymentStatusId] = source.[PaymentStatusId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([PaymentStatusId], [Name]) VALUES (source.[PaymentStatusId], source.[Name])
WHEN MATCHED THEN
    UPDATE SET target.[Name] = source.[Name];
GO

-------------------------------------------------------------------------------
-- 5.5  DiscountAppliesTo  (Restaurant.Application.Enums.DiscountAppliesTo)
-------------------------------------------------------------------------------
MERGE dbo.[DiscountAppliesTo] AS target
USING (VALUES
    (1, N'Restaurant'),
    (2, N'Branch'),
    (3, N'Product'),
    (4, N'Category'),
    (5, N'Coupon')
) AS source ([DiscountAppliesToId], [Name])
    ON target.[DiscountAppliesToId] = source.[DiscountAppliesToId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([DiscountAppliesToId], [Name]) VALUES (source.[DiscountAppliesToId], source.[Name])
WHEN MATCHED THEN
    UPDATE SET target.[Name] = source.[Name];
GO

-------------------------------------------------------------------------------
-- 5.6  Roles
--
--      SystemAdmin is also created by SeedData.SeedSystemAdminAsync at API
--      start-up; inserting it here first keeps the ids stable.
-------------------------------------------------------------------------------
MERGE dbo.[Roles] AS target
USING (VALUES
    (N'SystemAdmin', N'System administrator with highest privileges'),
    (N'SuperAdmin',  N'Owner of a head office - manages every branch'),
    (N'Admin',       N'Branch administrator - manages catalogue and staff'),
    (N'OrderTaker',  N'Takes and progresses customer orders'),
    (N'Rider',       N'Delivery rider'),
    (N'Customer',    N'Registered customer placing online orders')
) AS source ([RoleName], [RoleDescription])
    ON target.[RoleName] = source.[RoleName]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([RoleName], [RoleDescription], [IsActive])
    VALUES (source.[RoleName], source.[RoleDescription], 1);
GO

-------------------------------------------------------------------------------
-- 5.7  OrderStatusTransitionsRules
--
--      Drives OrderRepository.GetAvailableStatusesAsync: the admin UI only
--      offers the statuses reachable from the order's current status.
--      OrderTypeId 1 = Delivery, 2 = Pickup, 3 = DineIn, 4 = Takeaway.
-------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.[OrderStatusTransitionsRules])
BEGIN
    INSERT INTO dbo.[OrderStatusTransitionsRules]
        ([OrderTypeId], [FromStatusId], [ToStatusId], [RequiredRole], [IsActive])
    VALUES
        -- Delivery: Pending -> Confirmed -> Preparing -> OutForDelivery -> Delivered
        (1, 1, 2, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 1, 9, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 1, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 2, 3, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 2, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 3, 5, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (1, 5, 6, N'SystemAdmin,SuperAdmin,Admin,OrderTaker,Rider', 1),
        (1, 6, 10, N'SystemAdmin,SuperAdmin,Admin', 1),

        -- Pickup: Pending -> Confirmed -> Preparing -> ReadyForPickup -> Completed
        (2, 1, 2, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 1, 9, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 1, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 2, 3, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 2, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 3, 4, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 4, 7, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (2, 7, 10, N'SystemAdmin,SuperAdmin,Admin', 1),

        -- DineIn: Pending -> Confirmed -> Preparing -> Completed
        (3, 1, 2, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (3, 1, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (3, 2, 3, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (3, 3, 7, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),

        -- Takeaway: Pending -> Confirmed -> Preparing -> ReadyForPickup -> Completed
        (4, 1, 2, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (4, 1, 8, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (4, 2, 3, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (4, 3, 4, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1),
        (4, 4, 7, N'SystemAdmin,SuperAdmin,Admin,OrderTaker', 1);
END
GO


/* ===========================================================================
   SECTION 6 - OPTIONAL DEMO DATA
   ---------------------------------------------------------------------------
   A single head office with one branch, three categories, six products, the
   branch price list, a modifier group and an add-on group. This is exactly
   what the Angular customer app needs in order to render a menu.

   Set @SeedDemoData = 0 below to skip this section entirely.
   =========================================================================== */

DECLARE @SeedDemoData BIT = 1;

IF @SeedDemoData = 1 AND NOT EXISTS (SELECT 1 FROM dbo.[HeadOffice])
BEGIN
    PRINT 'Seeding demo data...';

    ---------------------------------------------------------------------------
    -- Head office + its address
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[Addresses]
        ([AddressLine1], [City], [State], [Country], [PostalCode], [IsDefault])
    VALUES
        (N'12 Mall Road', N'Lahore', N'Punjab', N'Pakistan', N'54000', 1);

    DECLARE @HoAddressId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[HeadOffice]
        ([Name], [Description], [PhoneNumber], [Email], [Website],
         [AddressId], [BusinessCategory], [IsActive], [CreatedAt], [IsDeleted])
    VALUES
        (N'Spice Route Restaurants',
         N'Demo head office created by the OOMS schema script',
         N'+924235000000', N'hello@spiceroute.test', N'https://spiceroute.test',
         @HoAddressId, N'Restaurant', 1, GETUTCDATE(), 0);

    DECLARE @HeadOfficeId INT = SCOPE_IDENTITY();

    ---------------------------------------------------------------------------
    -- Branch + its address + opening hours
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[Addresses]
        ([AddressLine1], [City], [State], [Country], [PostalCode], [IsDefault])
    VALUES
        (N'88 Gulberg Boulevard', N'Lahore', N'Punjab', N'Pakistan', N'54660', 1);

    DECLARE @BranchAddressId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[Branches]
        ([HeadOfficeId], [BranchName], [PhoneNumber], [Email], [AddressId],
         [Latitude], [Longitude], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
        (@HeadOfficeId, N'Gulberg Branch', N'+924235111222', N'gulberg@spiceroute.test',
         @BranchAddressId, 31.516100, 74.343300, 1, 0, GETUTCDATE());

    DECLARE @BranchId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[BranchTimings]
        ([BranchId], [BranchTimingName], [OpenTime], [CloseTime], [IsClosed], [SortOrder], [IsActive])
    VALUES
        (@BranchId, N'Monday',    '11:00', '23:00', 0, 1, 1),
        (@BranchId, N'Tuesday',   '11:00', '23:00', 0, 2, 1),
        (@BranchId, N'Wednesday', '11:00', '23:00', 0, 3, 1),
        (@BranchId, N'Thursday',  '11:00', '23:00', 0, 4, 1),
        (@BranchId, N'Friday',    '11:00', '00:00', 0, 5, 1),
        (@BranchId, N'Saturday',  '11:00', '00:00', 0, 6, 1),
        (@BranchId, N'Sunday',    '12:00', '22:00', 0, 7, 1);

    ---------------------------------------------------------------------------
    -- Tax (applied by CartRepository when totalling a cart)
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[Taxs]
        ([Code], [Name], [Rate], [IsPercentage], [IsCompound], [Priority],
         [PaymentTypeId], [PaymentType], [HeadOfficeId], [IsActive], [IsDeleted], [CreatedAt])
    VALUES
        (N'GST', N'General Sales Tax', 16.00, 1, 0, 1, 1, N'Cash', @HeadOfficeId, 1, 0, GETUTCDATE());

    DECLARE @TaxId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[BranchTaxes] ([BranchId], [TaxId], [Priority], [IsCompound], [IsActive])
    VALUES (@BranchId, @TaxId, 1, 0, 1);

    ---------------------------------------------------------------------------
    -- Categories
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[Categories]
        ([CategoryName], [Description], [ImageUrl], [HeadOfficeId], [IsActive], [IsDeleted])
    VALUES
        (N'Burgers', N'Flame grilled, served with fries',
         N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800', @HeadOfficeId, 1, 0),
        (N'Pizza',   N'Hand stretched dough, wood fired',
         N'https://images.unsplash.com/photo-1513104890138-7c749659a591?w=800', @HeadOfficeId, 1, 0),
        (N'Drinks',  N'Chilled soft drinks and fresh juices',
         N'https://images.unsplash.com/photo-1437418747212-8d9709afab22?w=800', @HeadOfficeId, 1, 0);

    DECLARE @CatBurgers INT = (SELECT [CategoryId] FROM dbo.[Categories] WHERE [CategoryName] = N'Burgers' AND [HeadOfficeId] = @HeadOfficeId);
    DECLARE @CatPizza   INT = (SELECT [CategoryId] FROM dbo.[Categories] WHERE [CategoryName] = N'Pizza'   AND [HeadOfficeId] = @HeadOfficeId);
    DECLARE @CatDrinks  INT = (SELECT [CategoryId] FROM dbo.[Categories] WHERE [CategoryName] = N'Drinks'  AND [HeadOfficeId] = @HeadOfficeId);

    ---------------------------------------------------------------------------
    -- Products
    ---------------------------------------------------------------------------
    DECLARE @Products TABLE ([ProductId] INT, [Name] NVARCHAR(200), [CategoryId] INT, [Price] DECIMAL(18,2));

    INSERT INTO dbo.[Products]
        ([ProductName], [Description], [Price], [ImageUrl], [HeadOfficeId], [IsAvailable], [IsDeleted])
    OUTPUT inserted.[ProductId], inserted.[ProductName], CAST(NULL AS INT), inserted.[Price] INTO @Products
    VALUES
        (N'Classic Beef Burger', N'180g beef patty, cheddar, lettuce, house sauce', 950.00,
         N'https://images.unsplash.com/photo-1550547660-d9450f859349?w=800', @HeadOfficeId, 1, 0),
        (N'Crispy Chicken Burger', N'Buttermilk fried chicken thigh, slaw, pickles', 890.00,
         N'https://images.unsplash.com/photo-1606755962773-d324e0a13086?w=800', @HeadOfficeId, 1, 0),
        (N'Margherita Pizza', N'San Marzano tomato, fior di latte, basil', 1250.00,
         N'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=800', @HeadOfficeId, 1, 0),
        (N'Pepperoni Pizza', N'Double pepperoni, mozzarella, oregano', 1450.00,
         N'https://images.unsplash.com/photo-1628840042765-356cda07504e?w=800', @HeadOfficeId, 1, 0),
        (N'Fresh Lime Soda', N'Lime, soda, mint - served over ice', 280.00,
         N'https://images.unsplash.com/photo-1437418747212-8d9709afab22?w=800', @HeadOfficeId, 1, 0),
        (N'Cola 500ml', N'Chilled bottle', 180.00,
         N'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=800', @HeadOfficeId, 1, 0);

    -- map each product to its category
    UPDATE @Products SET [CategoryId] =
        CASE
            WHEN [Name] LIKE N'%Burger%' THEN @CatBurgers
            WHEN [Name] LIKE N'%Pizza%'  THEN @CatPizza
            ELSE @CatDrinks
        END;

    INSERT INTO dbo.[CategoryProduct] ([CategoryId], [ProductId], [IsActive])
    SELECT [CategoryId], [ProductId], 1 FROM @Products;

    -- publish every product to the demo branch at its default price
    INSERT INTO dbo.[BranchProduct] ([BranchId], [ProductId], [Price], [IsActive])
    SELECT @BranchId, [ProductId], [Price], 1 FROM @Products;

    ---------------------------------------------------------------------------
    -- Modifier group: "Choose your size" (single select, required)
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[ModifierCategory] ([Name], [IsRequired], [Price], [HeadOfficeId], [IsActive])
    VALUES (N'Choose your size', 1, 0.00, @HeadOfficeId, 1);

    DECLARE @ModCatId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[Modifier] ([Name], [CategoryId], [DefaultPrice], [HeadOfficeId], [IsActive])
    VALUES
        (N'Regular', @ModCatId,   0.00, @HeadOfficeId, 1),
        (N'Large',   @ModCatId, 250.00, @HeadOfficeId, 1);

    -- attach the size group to both pizzas
    INSERT INTO dbo.[ProductModifier] ([ProductId], [ModifierId], [HeadOfficeId])
    SELECT p.[ProductId], m.[Id], @HeadOfficeId
    FROM @Products p
    CROSS JOIN dbo.[Modifier] m
    WHERE p.[Name] LIKE N'%Pizza%' AND m.[CategoryId] = @ModCatId;

    ---------------------------------------------------------------------------
    -- Add-on group: "Extras" (optional, up to 4)
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[AddOnCategory] ([Name], [MinSelect], [MaxSelect], [SortOrder], [HeadOfficeId], [IsActive])
    VALUES (N'Extras', 0, 4, 1, @HeadOfficeId, 1);

    DECLARE @AddOnCatId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.[AddOn] ([Name], [AddOnCategoryId], [AddOnUnitPrice], [HeadOfficeId], [IsActive], [IsDeleted])
    VALUES
        (N'Extra Cheese',      @AddOnCatId, 150.00, @HeadOfficeId, 1, 0),
        (N'Jalapenos',         @AddOnCatId,  80.00, @HeadOfficeId, 1, 0),
        (N'Crispy Bacon',      @AddOnCatId, 220.00, @HeadOfficeId, 1, 0),
        (N'Garlic Mayo Dip',   @AddOnCatId,  90.00, @HeadOfficeId, 1, 0);

    -- attach extras to burgers and pizzas
    INSERT INTO dbo.[ProductAddOn] ([ProductId], [AddOnId], [HeadOfficeId])
    SELECT p.[ProductId], a.[Id], @HeadOfficeId
    FROM @Products p
    CROSS JOIN dbo.[AddOn] a
    WHERE (p.[Name] LIKE N'%Burger%' OR p.[Name] LIKE N'%Pizza%')
      AND a.[AddOnCategoryId] = @AddOnCatId;

    ---------------------------------------------------------------------------
    -- Dine-in tables
    ---------------------------------------------------------------------------
    INSERT INTO dbo.[Table]
        ([TableNumber], [TableName], [TableLocation], [TableCapacity], [TableStatus],
         [BranchId], [HeadOfficeId], [IsActive], [IsDeleted])
    VALUES
        (N'T-01', N'Window 1', N'Ground floor', 2, N'Available', @BranchId, @HeadOfficeId, 1, 0),
        (N'T-02', N'Window 2', N'Ground floor', 4, N'Available', @BranchId, @HeadOfficeId, 1, 0),
        (N'T-03', N'Terrace',  N'First floor',  6, N'Available', @BranchId, @HeadOfficeId, 1, 0);

    PRINT 'Demo data seeded. HeadOfficeId = ' + CAST(@HeadOfficeId AS NVARCHAR(10))
        + ', BranchId = ' + CAST(@BranchId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT 'Demo data skipped (already present, or @SeedDemoData = 0).';
END
GO


/* ===========================================================================
   DONE
   ---------------------------------------------------------------------------
   The SystemAdmin login is NOT created here. It is seeded by the API on start
   up (Program.cs -> SeedData.SeedSystemAdminAsync) because the password hash
   must be produced with HMACSHA512 by the same code that verifies it.

       Username : systemadmin
       Email    : dev.aliqasim@gmail.com
       Password : SystemAdmin@123

   Verify the install:

       USE OOMS;
       SELECT COUNT(*) AS TableCount FROM sys.tables;               -- expect 53
       SELECT * FROM dbo.OrderStatuses ORDER BY OrderStatusId;      -- expect 10
       SELECT * FROM dbo.OrderStatusTransitionsRules;               -- expect 25
   =========================================================================== */
