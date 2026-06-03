using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Dtos;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace Restaurant.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        #region Get Cart
        public async Task<ApiResponse> GetByIdAsync(int? userId, Guid? GuestSessionToken = null)
        {
            var apiResponse = new ApiResponse();
            try
            {
                var cart = await _context.Carts
                    .Where(c => (
                                    (userId != null && c.UserId == userId) || (GuestSessionToken != null && c.GuestSessionToken == GuestSessionToken)
                                ) && (c.IsDeleted == null || c.IsDeleted == false))
                    .Select(c => new CartDto
                    {
                        CartId = c.CartId,
                        UserId = c.UserId,
                        GuestSessionToken = c.GuestSessionToken,
                        BranchId = c.BranchId ?? 0,
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        Items = c.CartItems.Select(ci => new CartItemDto
                        {
                            CartItemId = ci.CartItemId,
                            ProductId = ci.ProductId,
                            ProductName = _context.Products.FirstOrDefault(x => x.ProductId == ci.ProductId).ProductName,
                            ProductDescription = _context.Products.FirstOrDefault(x => x.ProductId == ci.ProductId).Description,
                            ProductImageUrl = _context.Products.FirstOrDefault(x => x.ProductId == ci.ProductId).ImageUrl,
                            Quantity = ci.Quantity ?? 0,
                            UnitPrice = ci.UnitPrice ?? 0,
                            TotalPrice = ci.TotalPrice ?? 0,
                            Instructions = ci.Instructions,

                            Modifiers = ci.CartItemModifiers.Select(m => new CartItemModifierDto
                            {
                                ModifierId = m.ModifierId ?? 0,
                                ModifierName = m.ModifierName,
                                ModifierPrice = m.ModifierPrice ?? 0
                            }).ToList(),

                            Addons = ci.CartItemAddons.Select(a => new CartItemAddonDto
                            {
                                AddOnId = a.AddOnId ?? 0,
                                AddOnName = a.AddOnName,
                                AddOnPrice = a.AddOnPrice ?? 0,
                                AddOnQuantity = a.AddOnQuantity ?? 0
                            }).ToList()

                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (cart == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Cart not found.";
                    return apiResponse;
                }

                // Subtotal
                cart.SubTotal = cart.Items.Sum(i =>
                    (i.UnitPrice * i.Quantity) +
                    i.Modifiers.Sum(m => m.ModifierPrice) +
                    i.Addons.Sum(a => a.AddOnPrice * a.AddOnQuantity)
                );

                //Taxes(branch level, if enabled)
                var branchTaxes = await (from bt in _context.BranchTaxes
                                         join t in _context.Taxs on bt.TaxId equals t.TaxId
                                         where bt.BranchId == cart.BranchId && bt.IsActive == true 
                                               && t.IsDeleted == false && t.IsActive == true
                                         orderby bt.Priority
                                         select new
                                         {
                                             t.TaxId,
                                             t.Code,
                                             t.Name,
                                             t.Rate,
                                             t.IsPercentage,
                                             t.IsCompound,
                                             bt.Priority,
                                             t.PaymentType,
                                             t.PaymentTypeId
                                         }

                                         ).ToListAsync();

                foreach (var tax in branchTaxes)
                {
                    var amount = tax.IsPercentage.Value
                        ? (cart.SubTotal * tax.Rate / 100)
                        : tax.Rate;

                    cart.Taxes.Add(new TaxDto
                    {
                        TaxId = tax.TaxId,
                        Code = tax.Code,
                        Name = tax.Name,
                        Rate = tax.Rate.Value,
                        IsPercentage = tax.IsPercentage.Value,
                        IsCompound = tax.IsCompound.Value,
                        Priority = tax.Priority.Value,
                        PaymentTypeId = tax.PaymentTypeId.Value,
                        PaymentType = tax.PaymentType,
                        Amount = amount.Value
                    });
                }

                // Grand Total (Apply the first tax that found Priority wise
                // A different tax can later be applied based on Payment Type selected by Customer
                cart.GrandTotal = cart.SubTotal;
                if (cart.Taxes.Count > 0)
                {
                    cart.GrandTotal = cart.SubTotal + cart.Taxes[0].Amount;
                }

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Cart retrieved successfully!";
                apiResponse.Data = cart;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return apiResponse;
        }
        #endregion

        #region Add Item
        public async Task<ApiResponse> AddItemAsync(AddCartItemRequest dto)
        {
            var apiResponse = new ApiResponse();
            try
            {
                // Find or create cart
                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c =>
                        (dto.UserId != null && c.UserId == dto.UserId) ||
                        (dto.GuestSessionToken != null && c.GuestSessionToken == dto.GuestSessionToken));

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = dto.UserId,
                        GuestSessionToken = dto.GuestSessionToken ?? Guid.NewGuid(),
                        BranchId = dto.BranchId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItem = new CartItem();

                // Check if product already exists in cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == dto.ProductId);
                if (existingItem != null)
                {
                    // Getting a reference to Existing Cart Item Id;
                    cartItem = existingItem;

                    existingItem.Quantity = dto.Quantity >= 0 ? dto.Quantity : existingItem.Quantity;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    existingItem.Instructions = dto.Instructions;
                }
                else
                {
                    var product = await _context.Products.FindAsync(dto.ProductId);
                    if (product == null)
                    {
                        apiResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                        apiResponse.Message = "Product not found.";
                        return apiResponse;
                    }

                    cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * dto.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Instructions = dto.Instructions
                    };

                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Getting a reference to the Currently Processing Cart Item Id
                var savedCartItemId = cartItem.CartItemId;

                // 3. Handle Modifiers
                if (dto.Modifiers != null && dto.Modifiers.Any())
                {
                    // clear old Modifiers
                    var oldModifiersList = await _context.CartItemModifiers.Where(x => x.CartItemId == savedCartItemId).ToListAsync();
                    if (oldModifiersList.Any())
                    {
                        _context.CartItemModifiers.RemoveRange(oldModifiersList);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var mod in dto.Modifiers)
                    {
                        var modifier = await _context.Modifiers.FindAsync(mod.ModifierId);
                        if (modifier != null)
                        {
                            var modItem = new CartItemModifier
                            {
                                CartItemId = savedCartItemId,
                                ModifierId = modifier.Id,
                                ModifierName = modifier.Name,
                                ModifierCategoryId = modifier.CategoryId,
                                //Quantity = mod.Quantity,
                                ModifierPrice = (modifier.DefaultPrice) * mod.Quantity
                            };
                            _context.CartItemModifiers.Add(modItem);
                            cartItem.TotalPrice += modItem.ModifierPrice; // add to total
                        }
                    }
                }

                // 4. Handle Addons
                if (dto.Addons != null && dto.Addons.Any())
                {
                    // clear old Addons
                    var oldAddonList = await _context.CartItemAddons.Where(x => x.CartItemId == savedCartItemId).ToListAsync();
                    if (oldAddonList.Any())
                    {
                        _context.CartItemAddons.RemoveRange(oldAddonList);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var add in dto.Addons)
                    {
                        var addon = await _context.AddOns.FindAsync(add.AddOnId);
                        if (addon != null)
                        {
                            var addItem = new CartItemAddon
                            {
                                CartItemId = savedCartItemId,
                                AddOnId = addon.Id,
                                AddOnName = addon.Name,
                                AddonCategoryId = addon.AddOnCategoryId,
                                AddOnQuantity = add.Quantity,
                                AddOnPrice = (addon.AddOnUnitPrice ?? 0) * add.Quantity
                            };
                            _context.CartItemAddons.Add(addItem);
                            cartItem.TotalPrice += addItem.AddOnPrice; // add to total
                        }
                    }
                }

                await _context.SaveChangesAsync();


                return await GetByIdAsync(cart.CartId); // return updated cart
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }
        #endregion

        #region Update Item
        //public async Task<ApiResponse> UpdateItemAsync(UpdateCartItemRequest dto)
        //{
        //    var apiResponse = new ApiResponse();
        //    try
        //    {
        //        var item = await _context.CartItems.FindAsync(dto.CartItemId);
        //        if (item == null)
        //        {
        //            apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
        //            apiResponse.Message = "Cart item not found.";
        //            return apiResponse;
        //        }

        //        item.Quantity = dto.Quantity;
        //        item.TotalPrice = item.UnitPrice * dto.Quantity;
        //        item.UpdatedAt = DateTime.UtcNow;

        //        await _context.SaveChangesAsync();

        //        return await GetByIdAsync(item.CartId.Value);
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
        //        apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
        //        return apiResponse;
        //    }
        //}
        #endregion

        #region Remove Item
        public async Task<ApiResponse> RemoveItemAsync(int cartItemId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                var item = await _context.CartItems.FindAsync(cartItemId);
                if (item == null)
                {
                    apiResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Cart item not found.";
                    return apiResponse;
                }

                // Remove Cart Item Modifier
                var cartItemModifiers = await _context.CartItemModifiers.Where(x => x.CartItemId == cartItemId).ToListAsync();
                if (cartItemModifiers.Count > 0)
                {
                    _context.CartItemModifiers.RemoveRange(cartItemModifiers);
                    await _context.SaveChangesAsync();
                }
                // Remove Cart Item Addons 
                var cartItemAddonss = await _context.CartItemAddons.Where(x => x.CartItemId == cartItemId).ToListAsync();
                if (cartItemAddonss.Count > 0)
                {
                    _context.CartItemAddons.RemoveRange(cartItemAddonss);
                    await _context.SaveChangesAsync();
                }
                // Now remove Cart Item

                var cartId = item.CartId.Value;

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                apiResponse.StatusCode = (int)HttpStatusCode.OK;
                apiResponse.Message = "Item removed successfully!";
                apiResponse.Data = new { };
                return apiResponse;
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }
        #endregion

        #region Clear Cart
        public async Task<ApiResponse> ClearCartAsync(int cartId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                var items = _context.CartItems.Where(ci => ci.CartId == cartId).ToList();
                foreach (var item in items)
                {
                    await RemoveItemAsync(item.CartItemId);
                }

                return await GetByIdAsync(cartId);
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = ex.InnerException?.Message ?? ex.Message;
                return apiResponse;
            }
        }
        #endregion
    }
}
