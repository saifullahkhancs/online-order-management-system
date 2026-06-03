using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Enums;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data.SqlClient;


namespace Restaurant.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContext;
        private readonly ICartRepository _cartRepository;
        private readonly IBaseRepository _baseRepository;

        public OrderRepository(AppDbContext context, UserContextService userContext, ICartRepository cartRepository, IBaseRepository baseRepository)
        {
            _context = context;
            _userContext = userContext;
            _cartRepository = cartRepository;
            _baseRepository = baseRepository;
        }

        public async Task<ApiResponse> PlaceOrderAsync(OrderDto dto)
        {
            var response = new ApiResponse();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //  Basic Validation
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto), "Order data cannot be null");

                if (dto.OrderItems == null || !dto.OrderItems.Any())
                    throw new InvalidOperationException("Order must contain at least one item");

                // Verify branch exists
                if (dto.BranchId == null || dto.BranchId <= 0)
                {
                    throw new InvalidOperationException("Invalid branch");
                }

                var isBranchExists = await _context.Branches.FirstOrDefaultAsync(x => x.BranchId == dto.BranchId && x.IsActive == true && x.IsDeleted == false);
                if (isBranchExists == null)
                {
                    throw new InvalidOperationException("Branch doesnot exist");
                }

                // Sitting Table at branch check
                if (dto.TableId == null || dto.TableId <= 0)
                {
                    dto.TableId = null;
                }
                else
                {
                    var isTableExists = await _context.Tables.FirstOrDefaultAsync(x => x.TableId == dto.TableId && x.BranchId == dto.BranchId && x.IsActive == true && x.IsDeleted == false);
                    if (isTableExists == null)
                    {
                        throw new InvalidOperationException("Invalid table selected");
                    }
                }



                //  Create Order Entity
                var orderEntity = new Order
                {
                    CustomerId = dto.CustomerId,
                    BranchId = dto.BranchId,
                    OrderTypeId = dto.OrderTypeId,
                    TableId = dto.TableId,
                    OrderDate = DateTime.UtcNow,
                    Subtotal = dto.Subtotal ?? 0,
                    TaxAmount = dto.TaxAmount ?? 0,
                    DiscountAmount = dto.DiscountAmount ?? 0,
                    DeliveryFee = dto.DeliveryFee ?? 0,
                    TotalAmount = dto.TotalAmount ?? 0,
                    DeliveryAddressId = dto.DeliveryAddressId,
                    DeliveryAddress = dto.DeliveryAddress,
                    DeliveryInstructions = dto.DeliveryInstructions,
                    GuestSessionToken = dto.GuestSessionToken,
                    GuestName = dto.GuestName,
                    GuestPhoneNumber = dto.GuestPhoneNumber,
                    GuestEmailAddress = dto.GuestEmailAddress,
                    GuestAddressLine1 = dto.GuestAddressLine1,
                    GuestAddressLine2 = dto.GuestAddressLine2,
                    GuestAddressLine3 = dto.GuestAddressLine3,
                    GuestCity = dto.GuestCity,
                    GuestPostalCode = dto.GuestPostalCode,
                    GuestCountry = dto.GuestCountry,
                    CreatedAt = DateTime.UtcNow,
                    //CreatedBy = dto.CreatedBy,
                    IsActive = true,
                    IsDeleted = false
                };

                // Temporary order number (can later be replaced with format e.g. ORD-20251009-1234)
                orderEntity.OrderNumber = Guid.NewGuid().ToString();

                // Add Order to DB
                await _context.Orders.AddAsync(orderEntity);
                await _context.SaveChangesAsync();

                // Add Order Items
                foreach (var itemObj in dto.OrderItems.Select((item, index) => new { item, index }))
                {
                    var item = itemObj.item;
                    var itemIndex = itemObj.index;
                    var orderItem = new Persistence.Models.OrderItem();

                    if (item.ProductId != null && item.ProductId > 0)
                    {
                        var isProductExist = await _context.BranchProducts
                                                    .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId
                                                                         && x.ProductId == item.ProductId && x.IsActive == true);
                        if (isProductExist == null)
                        {
                            throw new InvalidOperationException($"Item Invalid at index: {itemIndex}");
                        }
                        orderItem = new Persistence.Models.OrderItem
                        {
                            OrderId = orderEntity.OrderId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity ?? 1,
                            UnitPrice = item.UnitPrice ?? 0,
                            FinalPrice = item.FinalPrice ?? 0,
                            Instructions = item.Instructions,
                            DiscountAmount = item.DiscountAmount ?? 0
                        };
                        await _context.OrderItems.AddAsync(orderItem);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Item Invalid at index: {itemIndex}");
                    }


                    // Item Addons
                    if (item.ItemAddons != null && item.ItemAddons.Any())
                    {
                        foreach (var addonObj in item.ItemAddons.Select((addon, index) => new { addon, index }))
                        {
                            var addon = addonObj.addon;
                            var addonIdex = addonObj.index;

                            if (addon.AddonId != null && addon.AddonId > 0)
                            {
                                var isAddonExist = await _context.AddOns
                                                            .FirstOrDefaultAsync(x => x.Id == addon.AddonId && x.IsActive == true && x.IsDeleted == false);
                                if (isAddonExist == null)
                                {
                                    throw new InvalidOperationException($"Addon Invalid at index: {addonIdex}");
                                }

                                var orderAddon = new Persistence.Models.OrderItemAddon
                                {
                                    OrderItemId = orderItem.OrderItemId,
                                    AddonId = addon.AddonId,
                                    AddonName = addon.AddonName,
                                    AddonQuantity = addon.AddonQuantity,
                                    AddonPrice = addon.AddonPrice,
                                    AddonCategoryId = addon.AddonCategoryId
                                };
                                await _context.OrderItemAddons.AddAsync(orderAddon);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Addon Invalid at index: {addonIdex}");
                            }
                        }
                    }

                    //  Item Modifiers
                    if (item.ItemModifiers != null && item.ItemModifiers.Any())
                    {
                        foreach (var modObj in item.ItemModifiers.Select((mod, index) => new { mod, index }))
                        {
                            var mod = modObj.mod;
                            var modIdex = modObj.index;

                            if (mod.ModifierId != null && mod.ModifierId > 0)
                            {
                                var isModifierExist = await _context.Modifiers
                                                            .FirstOrDefaultAsync(x => x.Id == mod.ModifierId);
                                if (isModifierExist == null)
                                {
                                    throw new InvalidOperationException($"Modifier Invalid at index: {modIdex}");
                                }

                                var orderMod = new Persistence.Models.OrderItemModifier
                                {
                                    OrderItemId = orderItem.OrderItemId,
                                    ModifierId = mod.ModifierId,
                                    ModifierName = mod.ModifierName,
                                    ModifierPrice = mod.ModifierPrice
                                };
                                await _context.OrderItemModifiers.AddAsync(orderMod);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Modifier Invalid at index: {modIdex}");
                            }
                        }
                    }

                    //  Item Discounts
                    //if (item.ItemDiscounts != null && item.ItemDiscounts.Any())
                    //{
                    //    foreach (var disc in item.ItemDiscounts)
                    //    {
                    //        var orderDisc = new Persistence.Models.OrderItemDiscount
                    //        {
                    //            OrderItemId = orderItem.OrderItemId,
                    //            DiscountId = disc.DiscountId,
                    //            DiscountName = disc.DiscountName,
                    //            DiscountType = disc.DiscountType,
                    //            DiscountAmount = disc.DiscountAmount
                    //        };
                    //        await _context.OrderItemDiscounts.AddAsync(orderDisc);
                    //    }
                    //}

                    //  Item Taxes
                    //if (item.ItemTax != null && item.ItemTax.Any())
                    //{
                    //    foreach (var tax in item.ItemTax)
                    //    {
                    //        var orderTax = new Persistence.Models.OrderItemTaxis
                    //        {
                    //            OrderItemId = orderItem.OrderItemId,
                    //            TaxId = tax.TaxId,
                    //            TaxName = tax.TaxName,
                    //            TaxRate = tax.TaxRate,
                    //            TaxableAmount = tax.TaxableAmount,
                    //            TaxAmount = tax.TaxAmount
                    //        };
                    //        await _context.OrderItemTaxes.AddAsync(orderTax);
                    //    }
                    //}
                }

                await _context.SaveChangesAsync();

                // Add Order-Level Taxes & Discounts
                if (dto.OrderTaxes != null && dto.OrderTaxes.Any())
                {
                    foreach (var taxObj in dto.OrderTaxes.Select((tax, index) => new { tax, index }))
                    {
                        var tax = taxObj.tax;
                        var taxIndex = taxObj.index;

                        if (tax.TaxId != null && tax.TaxId > 0)
                        {
                            var isTaxExist = await _context.BranchTaxes
                                            .FirstOrDefaultAsync(x => x.BranchId == dto.BranchId && x.TaxId == tax.TaxId && x.IsActive == true);
                            if (isTaxExist == null)
                            {
                                throw new InvalidOperationException($"Tax Invalid at index: {taxIndex}");
                            }

                            await _context.OrderTaxes.AddAsync(new Persistence.Models.OrderTaxis
                            {
                                OrderId = orderEntity.OrderId,
                                TaxId = tax.TaxId,
                                TaxName = tax.TaxName,
                                TaxRate = tax.TaxRate,
                                TaxableAmount = tax.TaxableAmount,
                                TaxAmount = tax.TaxAmount
                            });
                        }
                        else
                        {
                            throw new InvalidOperationException($"Tax Invalid at index: {taxIndex}");
                        }
                    }
                }

                //if (dto.OrderDiscounts != null && dto.OrderDiscounts.Any())
                //{
                //    foreach (var d in dto.OrderDiscounts)
                //    {
                //        await _context.OrderDiscounts.AddAsync(new Persistence.Models.OrderDiscount
                //        {
                //            OrderId = orderEntity.OrderId,
                //            DiscountId = d.DiscountId,
                //            DiscountCode = d.DiscountCode,
                //            DiscountName = d.DiscountName,
                //            DiscountType = d.DiscountType,
                //            DiscountAmount = d.DiscountAmount
                //        });
                //    }
                //}

                await _context.SaveChangesAsync();

                // Create Payment Record (Cash)
                var payment = new Payment
                {
                    OrderId = orderEntity.OrderId,
                    PaymentMethod = "Cash",
                    AmountPaid = orderEntity.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentDate = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();


                // Update Order status immediatedly for direct cash payment orders
                // Create Initial Status Record
                var initialStatus = new OrderStatusHistory
                {
                    OrderId = orderEntity.OrderId,
                    NewOrderStatusId = 1,
                    NewStatus = "Pending",
                    ChangedAt = DateTime.UtcNow,
                    //ChangedByUserId = dto.CreatedBy
                };
                await _context.OrderStatusHistories.AddAsync(initialStatus);

                // Update Order Status
                orderEntity.OrderStatusId = (int)Application.Enums.OrderStatus.Pending;
                orderEntity.OrderStatus = "Pending";
                orderEntity.PaymentId = payment.PaymentId;
                orderEntity.PaymentStatus = payment.PaymentStatus;
                _context.Orders.Update(orderEntity);
                await _context.SaveChangesAsync();


                // clear customer cart
                var getUserCart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == dto.CustomerId || x.GuestSessionToken == dto.GuestSessionToken);
                if (getUserCart != null)
                {
                    await _cartRepository.ClearCartAsync(getUserCart.CartId);
                }

                //  Commit Transaction
                await transaction.CommitAsync();



                //  Return Response DTO
                var data = (await GetOrderByIdAsync(orderEntity.OrderId)).Data;
                response.StatusCode = (int)HttpStatusCode.Created;
                response.Message = "Order processed successfully.";
                response.Data = data;
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // log exception (Serilog / ILogger)
                //throw new Exception($"Error while placing order: {ex.Message}", ex);
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                return response;
            }
        }

        public async Task<ApiResponse> GetOrderStatusAsync(long orderId)
        {
            var response = new ApiResponse();

            try
            {
                var orderStatus = await (
                    from o in _context.Orders
                    join s in _context.OrderStatuses on o.OrderStatusId equals s.OrderStatusId
                    where o.OrderId == orderId
                    select new OrderStatusDto
                    {
                        OrderId = o.OrderId,
                        OrderStatus = s.Name,
                        LastUpdated = o.UpdatedAt ?? o.CreatedAt ?? DateTime.UtcNow
                    }
                ).AsNoTracking().FirstOrDefaultAsync();

                if (orderStatus == null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Order not found.";
                    response.Data = null;
                    return response;
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.Message = "Order status fetched successfully.";
                response.Data = orderStatus;
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                response.Data = null;
                return response;
            }
        }

        public async Task<ApiResponse> GetOrderByIdAsync(long orderId, Guid? guestSessionToken = null, long? customerId = null)
        {
            var response = new ApiResponse();
            try
            {

                var userId = _userContext.GetUserId();

                // Fetch the order including all relationships
                var query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OrderItemAddons)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OrderItemModifiers)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    //.Include(o => o.OrderItems)
                    //    .ThenInclude(oi => oi.OrderItemDiscounts)
                    //.Include(o => o.OrderItems)
                    //    .ThenInclude(oi => oi.OrderItemTaxes)
                    .Include(o => o.OrderTaxes)
                    //.Include(o => o.OrderDiscounts)
                    .Include(o => o.Payments)
                    .Include(o => o.OrderStatusHistories)
                    .AsQueryable();

                // Security filter — match either CustomerId or GuestSessionToken
                if (customerId.HasValue && customerId.Value > 0)
                    query = query.Where(o => o.OrderId == orderId && o.CustomerId == customerId.Value);
                else if (guestSessionToken.HasValue)
                    query = query.Where(o => o.OrderId == orderId && o.GuestSessionToken == guestSessionToken.Value);
                else
                    query = query.Where(o => o.OrderId == orderId);

                var order = await query.FirstOrDefaultAsync();

                if (order == null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Order not found.";
                    return response;
                }

                // Map to DTO
                var dto = new OrderDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    BranchId = order.BranchId.Value,
                    OrderTypeId = order.OrderTypeId,
                    OrderType = _context.OrderTypes.FirstOrDefault(x => x.OrderTypeId == order.OrderTypeId)?.Name,
                    OrderDate = order.OrderDate,
                    TableId = order.TableId,
                    Subtotal = order.Subtotal,
                    TaxAmount = order.TaxAmount,
                    DiscountAmount = order.DiscountAmount,
                    DeliveryFee = order.DeliveryFee,
                    TotalAmount = order.TotalAmount,
                    //PaymentId = order.PaymentId,
                    PaymentStatus = order.PaymentStatus,
                    //OrderStatusId = order.OrderStatusId,
                    OrderStatus = _context.OrderStatuses.FirstOrDefault(x => x.OrderStatusId == order.OrderStatusId)?.Name,
                    //DeliveryAddressId = order.DeliveryAddressId,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryInstructions = order.DeliveryInstructions,
                    GuestSessionToken = order.GuestSessionToken,
                    GuestName = order.GuestName,
                    GuestPhoneNumber = order.GuestPhoneNumber,
                    GuestEmailAddress = order.GuestEmailAddress,
                    GuestAddressLine1 = order.GuestAddressLine1,
                    GuestAddressLine2 = order.GuestAddressLine2,
                    GuestAddressLine3 = order.GuestAddressLine3,
                    GuestCity = order.GuestCity,
                    GuestPostalCode = order.GuestPostalCode,
                    GuestCountry = order.GuestCountry,
                    CreatedAt = order.CreatedAt,
                    //UpdatedAt = order.UpdatedAt,
                    //IsActive = order.IsActive,
                    //IsDeleted = order.IsDeleted,
                    OrderItems = order.OrderItems.Select(oi => new Application.Dtos.OrderItem
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        ProductName = oi.ProductName,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        FinalPrice = oi.FinalPrice,
                        Instructions = oi.Instructions,
                        DiscountAmount = oi.DiscountAmount,
                        ImageUrl = oi.Product?.ImageUrl ?? null,
                        ItemAddons = oi.OrderItemAddons?.Select(a => new Application.Dtos.OrderItemAddon
                        {
                            OrderItemAddonId = a.OrderItemAddonId,
                            OrderItemId = a.OrderItemId,
                            AddonId = a.AddonId,
                            AddonName = a.AddonName,
                            AddonQuantity = a.AddonQuantity,
                            AddonPrice = a.AddonPrice,
                            AddonCategoryId = a.AddonCategoryId
                        }).ToList() ?? new(),
                        ItemModifiers = oi.OrderItemModifiers?.Select(m => new Application.Dtos.OrderItemModifier
                        {
                            OrderItemModifierId = m.OrderItemModifierId,
                            OrderItemId = m.OrderItemId,
                            ModifierId = m.ModifierId,
                            ModifierName = m.ModifierName,
                            ModifierPrice = m.ModifierPrice
                        }).ToList() ?? new(),
                        //ItemDiscounts = oi.OrderItemDiscounts?.Select(d => new OrderItemDiscount
                        //{
                        //    OrderItemDiscountId = d.OrderItemDiscountId,
                        //    DiscountId = d.DiscountId,
                        //    DiscountName = d.DiscountName,
                        //    DiscountType = d.DiscountType,
                        //    DiscountAmount = d.DiscountAmount
                        //}).ToList() ?? new(),
                        //ItemTax = oi.OrderItemTaxes?.Select(t => new OrderItemTax
                        //{
                        //    OrderItemTaxId = t.OrderItemTaxId,
                        //    TaxId = t.TaxId,
                        //    TaxName = t.TaxName,
                        //    TaxRate = t.TaxRate,
                        //    TaxableAmount = t.TaxableAmount,
                        //    TaxAmount = t.TaxAmount
                        //}).ToList() ?? new()
                    }).ToList(),
                    OrderTaxes = order.OrderTaxes.Select(t => new OrderTax
                    {
                        OrderTaxId = t.OrderTaxId,
                        OrderId = t.OrderId,
                        TaxId = t.TaxId,
                        TaxName = t.TaxName,
                        TaxRate = t.TaxRate,
                        TaxableAmount = t.TaxableAmount,
                        TaxAmount = t.TaxAmount
                    }).ToList(),
                    //OrderDiscounts = order.OrderDiscounts.Select(d => new OrderDiscount
                    //{
                    //    OrderDiscountId = d.OrderDiscountId,
                    //    OrderId = d.OrderId,
                    //    DiscountId = d.DiscountId,
                    //    DiscountCode = d.DiscountCode,
                    //    DiscountName = d.DiscountName,
                    //    DiscountType = d.DiscountType,
                    //    DiscountAmount = d.DiscountAmount
                    //}).ToList()
                };

                response.StatusCode = (int)HttpStatusCode.OK;
                response.Message = "Order details retrieved successfully.";
                response.Data = dto;
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                return response;
            }
        }

        public async Task<ApiResponse> CancelOrderAsync(long orderId, Guid? guestSessionToken = null, string? cancelReason = null)
        {
            var response = new ApiResponse();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userContext.GetUserId();
                int? customerId = null;
                //if (userId != null)
                //{
                //    var customerObj = await _context.Customers.FirstOrDefaultAsync(x => x.UserId == userId);
                //    if (customerObj != null) 
                //    {
                //        customerId = customerObj.CustomerId;
                //    }
                //}
                // Fetch order with validation
                var orderQuery = _context.Orders
                    .Include(o => o.OrderStatusHistories)
                    .Include(o => o.Payments)
                    .Where(o => o.OrderId == orderId &&
                        (o.IsActive.HasValue && o.IsActive == true) &&
                        (o.IsDeleted.HasValue && o.IsDeleted == false));

                if (customerId.HasValue && customerId.Value > 0)
                    orderQuery = orderQuery.Where(o => o.CustomerId == customerId.Value);
                else if (guestSessionToken.HasValue)
                    orderQuery = orderQuery.Where(o => o.GuestSessionToken == guestSessionToken.Value);

                var order = await orderQuery.FirstOrDefaultAsync();

                if (order == null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Order not found.";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(order.OrderStatus) || order.OrderStatusId <= 0)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Something went wrong. Cannot process request.";
                    return response;
                }

                int cancelledOrderStatusId = (int)Application.Enums.OrderStatus.Cancelled;
                int preparedOrderStatusId = (int)Application.Enums.OrderStatus.Preparing;
                int dispatchedOrderStatusId = (int)Application.Enums.OrderStatus.OutForDelivery; // Dispatched
                int deliveredOrderStatusId = (int)Application.Enums.OrderStatus.Delivered;

                // Check if already canceled or not cancelable
                if (order.OrderStatus == "Cancelled" || order.OrderStatusId == (int)Application.Enums.OrderStatus.Cancelled)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Order is already cancelled.";
                    return response;
                }

                //if ( (order.OrderStatus is "Prepared" or "Dispatched" or "Delivered") || 
                // (order.OrderStatusId == preparedOrderStatusId || 
                // order.OrderStatusId == dispatchedOrderStatusId || 
                // order.OrderStatusId == deliveredOrderStatusId) )
                //{
                //    response.StatusCode = (int)HttpStatusCode.BadRequest;
                //    response.Message = $"Order cannot be cancelled once it’s {order.OrderStatus.ToLower()}.";
                //    return response;
                //}

                if ((order.OrderStatus == "Pending" || order.OrderStatus == "Confirmed") ||
                    (order.OrderStatusId == 1 || order.OrderStatusId == 2))
                {
                    // continue
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = $"Order cannot be cancelled once it’s {order.OrderStatus.ToLower()}.";
                    return response;
                }


                // Capture Order Status before modification
                var oldOrderStatus = order.OrderStatus;
                var oldOrderStatusId = order.OrderStatusId;

                // Update order status
                order.OrderStatus = "Cancelled";
                order.OrderStatusId = (int)Application.Enums.OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow; // TODO: Add UpdatedBy
                

                // Update payment status (if prepaid)
                var payment = order.Payments.FirstOrDefault();
                if (payment != null && payment.PaymentMethod != "Cash")
                {
                    payment.PaymentStatus = "RefundPending";
                    _context.Payments.Update(payment);
                }

                // Create cancellation history record
                var cancelHistory = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    OldStatus = oldOrderStatus,
                    OldOrderStatusId = oldOrderStatusId,
                    NewStatus = order.OrderStatus,
                    NewOrderStatusId = order.OrderStatusId,
                    ChangedAt = DateTime.UtcNow,
                    Remarks = string.IsNullOrWhiteSpace(cancelReason) ? "Cancelled by customer" : cancelReason
                };

                await _context.OrderStatusHistories.AddAsync(cancelHistory);

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated order info
                var updated = await GetOrderByIdAsync(order.OrderId, guestSessionToken, customerId);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Message = "Order cancelled successfully.";
                response.Data = updated.Data;
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = ex.InnerException?.Message ?? ex.Message;
                return response;
            }
        }


        #region ----------------------- Manage Orders API ----------------------------
        public async Task<ApiResponse> GetLiveOrdersAsync()
        {
            var apiResponse = new ApiResponse();

            try
            {
                var userId = _userContext.GetUserId().Value;

                // Step 1: Role check
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
                var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

                if (roleCheck == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "Unauthorized User";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                // Step 2: Get branchIds for the user
                List<int> branchIds = new();

                if (roleCheck.IsSystemAdmin)
                {
                    // SystemAdmin => can see all branches
                    branchIds = await _context.Branches.Select(b => b.BranchId).ToListAsync();
                }
                else if (roleCheck.IsSuperAdmin)
                {

                    var headOfficeId = await _context.Users
                        .Where(u => u.UserId == userId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    branchIds = await _context.Branches
                        .Where(b => b.HeadOfficeId == headOfficeId)
                        .Select(b => b.BranchId)
                        .ToListAsync();
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {

                    branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == userId && ub.IsActive == true &&
                              (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();
                }
                else
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "User does not have permission to view live orders.";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                if (!branchIds.Any())
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "No branches found for this user.";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                // Step 3: Active statuses
                var activeStatuses = new int[]
                {
                    (int)Application.Enums.OrderStatus.Pending,
                    (int)Application.Enums.OrderStatus.Confirmed,
                    (int)Application.Enums.OrderStatus.Preparing,
                    (int)Application.Enums.OrderStatus.ReadyForPickup,
                    (int)Application.Enums.OrderStatus.OutForDelivery
                };

                // Step 4: Query live orders
                var orders = await (
                    from o in _context.Orders

                    // Join Branch to Get Branch Details
                    join br in _context.Branches on o.BranchId equals br.BranchId into brj
                    from br in brj.DefaultIfEmpty()

                     // Join Branch Address
                    join ba in _context.Addresses on br.AddressId equals ba.AddressId into baj
                    from ba in baj.DefaultIfEmpty()

                     // Join HeadOffice (via Branch)
                    join h in _context.HeadOffices on br.HeadOfficeId equals h.HeadOfficeId into hoj
                    from h in hoj.DefaultIfEmpty()

                     // Join HeadOffice Address
                    join ha in _context.Addresses on h.AddressId equals ha.AddressId into haj
                    from ha in haj.DefaultIfEmpty()

                     // Join with Customers (left join)
                    join c in _context.Customers on o.CustomerId equals c.CustomerId into co
                    from c in co.DefaultIfEmpty()

                     // Join with lookup tables (left joins)
                    join ot in _context.OrderTypes on o.OrderTypeId equals ot.OrderTypeId into otj
                    from ot in otj.DefaultIfEmpty()

                    join os in _context.OrderStatuses on o.OrderStatusId equals os.OrderStatusId into osj
                    from os in osj.DefaultIfEmpty()

                    join pm in _context.Payments on o.PaymentId equals pm.PaymentId into pmj
                    from pm in pmj.DefaultIfEmpty()

                    where branchIds.Contains(o.BranchId.Value)
                       && activeStatuses.Contains(o.OrderStatusId.Value)

                    orderby o.CreatedAt descending //,os.OrderStatusId ascending

                    select new LiveOrderDto
                    {
                        OrderId = o.OrderId,
                        OrderNumber = o.OrderNumber ?? "N/A",

                        //  Use customer info if exists, else fallback to anonymous guest info
                        CustomerId = c.CustomerId,
                        CustomerName = c != null ? c.FullName ?? "Guest" : "Guest",
                        CustomerPhone = c != null ? c.PhoneNumber ?? "N/A" : "N/A",

                        GuestSessionToken  = o.GuestSessionToken,
                        GuestName  = o.GuestName ,
                        GuestPhoneNumber  = o.GuestPhoneNumber ,
                        GuestEmailAddress  = o.GuestEmailAddress,
                        GuestAddressLine1  = o.GuestAddressLine1,
                        GuestAddressLine2  = o.GuestAddressLine2,
                        GuestAddressLine3  = o.GuestAddressLine3,
                        GuestCity  = o.GuestCity,
                        GuestPostalCode  = o.GuestPostalCode,
                        GuestCountry  = o.GuestCountry,


                        SubTotal = o.Subtotal ?? 0,
                        Total = o.TotalAmount ?? 0,

                        //  Map from lookup tables instead of enums
                        OrderType = ot != null ? ot.Name : "Unknown",
                        OrderStatus = os != null ? os.Name : "Unknown",
                        PaymentStatus = pm != null ? pm.PaymentStatus : "Unknown",
                        PaymentMethod = pm != null ? pm.PaymentMethod : "Unknown",

                        CreatedAt = o.CreatedAt.Value,

                        // Show branch Info
                        Branch = br != null ? new BranchDto 
                        {
                            BranchId = br.BranchId,
                            BranchName = br.BranchName,
                            HeadOfficeId  = br.HeadOfficeId.Value,
                            PhoneNumber  = br.PhoneNumber ,
                            Email  = br.Email,
                            Latitude  = br.Latitude,
                            Longitude  = br.Longitude,
                            //  Branch Address
                            BranchAddress = ba == null ? null : new AddressDto
                            {
                                AddressId = ba.AddressId,
                                AddressLine1 = ba.AddressLine1,
                                AddressLine2 = ba.AddressLine2,
                                AddressLine3 = ba.AddressLine3,
                                City = ba.City,
                                Country = ba.Country,
                                PostalCode = ba.PostalCode
                            },
                        } : null,

                        //  Add HeadOffice with Address
                        HeadOffice = h == null ? null : new HeadOfficeDto
                        {
                            HeadOfficeId = h.HeadOfficeId,
                            Name = h.Name,
                            Description = h.Description,
                            PhoneNumber = h.PhoneNumber,
                            Email = h.Email,
                            Website = h.Website,
                            BusinessCategory = h.BusinessCategory,
                            CreatedAt = h.CreatedAt,
                            CreatedBy = h.CreatedBy,
                            UpdatedAt = h.UpdatedAt,
                            UpdatedBy = h.UpdatedBy,
                            IsActive = h.IsActive,
                            IsDeleted = h.IsDeleted,
                            HeadOfficeAddress = ha == null ? null : new AddressDto
                            {
                                AddressId = ha.AddressId,
                                AddressLine1 = ha.AddressLine1,
                                AddressLine2 = ha.AddressLine2,
                                AddressLine3 = ha.AddressLine3,
                                City = ha.City,
                                State = ha.State,
                                Country = ha.Country,
                                PostalCode = ha.PostalCode,
                                IsDefault = ha.IsDefault ?? false
                            }
                        },


                        //  Include product items, addons, and modifiers
                        Items = (
                            //from i in _context.OrderItems
                            //where i.OrderId == o.OrderId
                            from i in _context.OrderItems
                            join p in _context.Products on i.ProductId equals p.ProductId into prodGroup
                            from p in prodGroup.DefaultIfEmpty() // left join
                            where i.OrderId == o.OrderId
                            select new LiveOrderItemDto
                            {
                                ProductName = i.ProductName,
                                Quantity = i.Quantity ?? 0,
                                Price = i.FinalPrice ?? 0,
                                ImageUrl = p.ImageUrl ?? null,

                                // Addons
                                Addons = (
                                    from a in _context.OrderItemAddons
                                    where a.OrderItemId == i.OrderItemId
                                    select new AddonDto
                                    {
                                        AddonName = a.AddonName,
                                        AddonQuantity = a.AddonQuantity ?? 1,
                                        AddonPrice = a.AddonPrice ?? 0
                                    }).ToList(),

                                // Modifiers
                                Modifiers = (
                                    from m in _context.OrderItemModifiers
                                    where m.OrderItemId == i.OrderItemId
                                    select new ModifierDto
                                    {
                                        ModifierName = m.ModifierName,
                                        ModifierPrice = m.ModifierPrice ?? 0
                                    }).ToList()
                            }).ToList()
                    })
                    .AsNoTracking()
                    .ToListAsync();



                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Live orders fetched successfully.";
                apiResponse.Data = orders;
                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                apiResponse.Data = null;
                return apiResponse;
            }
        }

        public async Task<ApiResponse> GetAllOrdersAsync()
        {
            var apiResponse = new ApiResponse();

            try
            {
                var userId = _userContext.GetUserId().Value;

                // Step 1: Role check
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId);
                var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;

                if (roleCheck == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "Unauthorized User";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                // Step 2: Get branchIds for the user
                List<int> branchIds = new();

                if (roleCheck.IsSystemAdmin)
                {
                    // SystemAdmin => can see all branches
                    branchIds = await _context.Branches.Select(b => b.BranchId).ToListAsync();
                }
                else if (roleCheck.IsSuperAdmin)
                {

                    var headOfficeId = await _context.Users
                        .Where(u => u.UserId == userId)
                        .Select(u => u.HeadOfficeId)
                        .FirstOrDefaultAsync();

                    branchIds = await _context.Branches
                        .Where(b => b.HeadOfficeId == headOfficeId)
                        .Select(b => b.BranchId)
                        .ToListAsync();
                }
                else if (roleCheck.IsAdmin || roleCheck.IsOrderTaker)
                {

                    branchIds = await (
                        from ub in _context.UserBranches
                        where ub.UserId == userId && ub.IsActive == true &&
                              (ub.IsDeleted == null || ub.IsDeleted == false)
                        select ub.BranchId
                    ).Distinct().ToListAsync();
                }
                else
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "User does not have permission to view live orders.";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                if (!branchIds.Any())
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "No branches found for this user.";
                    apiResponse.Data = new List<OrderDto>();
                    return apiResponse;
                }

                // Step 3: Active statuses
                //var activeStatuses = new int[]
                //{
                //    (int)Application.Enums.OrderStatus.Pending,
                //    (int)Application.Enums.OrderStatus.Confirmed,
                //    (int)Application.Enums.OrderStatus.Preparing,
                //    (int)Application.Enums.OrderStatus.ReadyForPickup,
                //    (int)Application.Enums.OrderStatus.OutForDelivery
                //};

                // Step 4: Query live orders
                var orders = await (
                    from o in _context.Orders
                    // Join Branch to Get Branch Details
                    join br in _context.Branches on o.BranchId equals br.BranchId into brj
                    from br in brj.DefaultIfEmpty()

                    // Join with Customers (left join)
                    join c in _context.Customers on o.CustomerId equals c.CustomerId into co
                    from c in co.DefaultIfEmpty()

                    // Join with lookup tables (left joins)
                    join ot in _context.OrderTypes on o.OrderTypeId equals ot.OrderTypeId into otj
                    from ot in otj.DefaultIfEmpty()

                    join os in _context.OrderStatuses on o.OrderStatusId equals os.OrderStatusId into osj
                    from os in osj.DefaultIfEmpty()

                    join pm in _context.Payments on o.PaymentId equals pm.PaymentId into pmj
                    from pm in pmj.DefaultIfEmpty()

                    where branchIds.Contains(o.BranchId.Value)
                       //&& activeStatuses.Contains(o.OrderStatusId.Value)

                    orderby o.CreatedAt descending //,os.OrderStatusId ascending

                    select new LiveOrderDto
                    {
                        OrderId = o.OrderId,
                        OrderNumber = o.OrderNumber ?? "N/A",

                        //  Use customer info if exists, else fallback to anonymous guest info
                        CustomerName = c != null ? c.FullName ?? o.GuestName : o.GuestName ?? "Guest",
                        CustomerPhone = c != null ? c.PhoneNumber ?? o.GuestPhoneNumber : o.GuestPhoneNumber ?? "N/A",

                        SubTotal = o.Subtotal ?? 0,
                        Total = o.TotalAmount ?? 0,

                        //  Map from lookup tables instead of enums
                        OrderType = ot != null ? ot.Name : "Unknown",
                        OrderStatus = os != null ? os.Name : "Unknown",
                        PaymentStatus = pm != null ? pm.PaymentStatus : "Unknown",
                        PaymentMethod = pm != null ? pm.PaymentMethod : "Unknown",

                        CreatedAt = o.CreatedAt.Value,
                        // Show branch Info
                        Branch = br != null ? new BranchDto
                        {
                            BranchId = br.BranchId,
                            BranchName = br.BranchName,

                            HeadOfficeId = br.HeadOfficeId.Value,
                            PhoneNumber = br.PhoneNumber,
                            Email = br.Email,
                            Latitude = br.Latitude,
                            Longitude = br.Longitude
                        } : null,

                        //  Include product items, addons, and modifiers
                        Items = (
                            //from i in _context.OrderItems
                            //where i.OrderId == o.OrderId
                            from i in _context.OrderItems
                            join p in _context.Products on i.ProductId equals p.ProductId into prodGroup
                            from p in prodGroup.DefaultIfEmpty() // left join
                            where i.OrderId == o.OrderId
                            select new LiveOrderItemDto
                            {
                                ProductName = i.ProductName,
                                Quantity = i.Quantity ?? 0,
                                Price = i.FinalPrice ?? 0,
                                ImageUrl = p.ImageUrl ?? null,

                                // Addons
                                Addons = (
                                    from a in _context.OrderItemAddons
                                    where a.OrderItemId == i.OrderItemId
                                    select new AddonDto
                                    {
                                        AddonName = a.AddonName,
                                        AddonQuantity = a.AddonQuantity ?? 1,
                                        AddonPrice = a.AddonPrice ?? 0
                                    }).ToList(),

                                // Modifiers
                                Modifiers = (
                                    from m in _context.OrderItemModifiers
                                    where m.OrderItemId == i.OrderItemId
                                    select new ModifierDto
                                    {
                                        ModifierName = m.ModifierName,
                                        ModifierPrice = m.ModifierPrice ?? 0
                                    }).ToList()
                            }).ToList()
                    })
                    .AsNoTracking()
                    .ToListAsync();



                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "All orders fetched successfully.";
                apiResponse.Data = orders;
                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                apiResponse.Data = null;
                return apiResponse;
            }
        }

        public async Task<ApiResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest dto)
        {
            var apiResponse = new ApiResponse();

            try
            {
                var userId = _userContext.GetUserId().Value;

                // Step 1: Get user role
                var roleCheckResponse = await _baseRepository.GetRoleCheckResultAsync(userId!);
                var roleCheck = roleCheckResponse.Data as BaseRepository.RoleCheckResult;
                if (roleCheck == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.OK;
                    apiResponse.Message = "Unauthorized User";
                    apiResponse.Data = "Unauthorized User";
                    return apiResponse;
                }

                // Step 2: Find order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);

                if (order == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Order not found.";
                    return apiResponse;
                }

                var currentStatusId = order.OrderStatusId;
                var newStatusId = dto.NewStatusId;

                //// Step 3: Validate transition using EF Core (instead of ExecuteScalar)
                //var isAllowed = await _context.OrderStatusTransitionsRules
                //    .AnyAsync(r =>
                //        (r.OrderTypeId == order.OrderTypeId || r.OrderTypeId == null) &&
                //        r.FromStatusId == currentStatusId.Value &&
                //        r.ToStatusId == newStatusId &&
                //        r.IsActive.Value);

                //if (!isAllowed)
                //{
                //    apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                //    apiResponse.Message = $"Transition not allowed from status ID {currentStatusId} to {newStatusId} .";
                //    return apiResponse;
                //}

                // Step 4: Update order status
                order.OrderStatusId = newStatusId;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = userId;

                _context.Orders.Update(order);

                // Step 5: Log status history
                var history = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    OldOrderStatusId = currentStatusId,
                    NewOrderStatusId = newStatusId,
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    Remarks = dto.Remarks
                };

                await _context.OrderStatusHistories.AddAsync(history);
                await _context.SaveChangesAsync();

                // Step 6: Prepare response
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Order status updated successfully!";
                apiResponse.Data = new
                {
                    OrderId = order.OrderId,
                    OldStatusId = currentStatusId,
                    NewStatusId = newStatusId,
                    UpdatedBy = userId,
                    UpdatedAt = order.UpdatedAt
                };

                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                apiResponse.Data = null;
                return apiResponse;
            }
        }


        public async Task<ApiResponse> GetAvailableStatusesAsync(int orderId)
        {
            var apiResponse = new ApiResponse();

            try
            {
                // Step 1: Get order info
                var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Order not found.";
                    return apiResponse;
                }

                var orderTypeId = order.OrderTypeId;
                var fromStatusId = order.OrderStatusId;

                // Step 2: Query using EF Core
                var query =
                    from t in _context.OrderStatusTransitionsRules
                    join s in _context.OrderStatuses on t.ToStatusId equals s.OrderStatusId
                    where (t.OrderTypeId == orderTypeId || t.OrderTypeId == null)
                          && t.FromStatusId == fromStatusId.Value
                          && t.IsActive.Value
                    select new AvailableStatusOption
                    {
                        StatusId = s.OrderStatusId,
                        StatusName = s.Name
                    };

                // Step 3: Execute
                var result = await query
                    .Distinct()
                    .AsNoTracking()
                    .ToListAsync();

                // Step 6: Prepare response
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Order status retrieved successfully!";
                apiResponse.Data = result;

                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                apiResponse.Data = null;
                return apiResponse;
            }

        }

        public async Task<ApiResponse> GetAllOrderStatuses(int? headOfficeId)
        {
            var apiResponse = new ApiResponse();

            try
            {
                if(headOfficeId != null && headOfficeId > 0)
                {
                    var isValidHeadOfficeId = await _context.HeadOffices
                                            .AsNoTracking()
                                            .AnyAsync(h => h.HeadOfficeId == headOfficeId && h.IsActive == true && h.IsDeleted == false);
                    if(!isValidHeadOfficeId)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                        apiResponse.Message = "Invalid HeadOffice Provided.";
                        return apiResponse;
                    }
                }
                

                // Step 2: Query using EF Core
                var statusList =
                    await (from s in _context.OrderStatuses
                    select new AvailableStatusOption
                    {
                        StatusId = s.OrderStatusId,
                        StatusName = s.Name
                    }).ToListAsync();

                // Step 6: Prepare response
                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Order status retrieved successfully!";
                apiResponse.Data = statusList;

                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                apiResponse.Data = null;
                return apiResponse;
            }

        }
        #endregion

    }

}
