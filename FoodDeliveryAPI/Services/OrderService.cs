using AutoMapper;
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using FoodDeliveryAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FoodDeliveryAPI.Services;

public class OrderService(
    FoodDeliveryDbContext context,
    IMapper mapper) : IOrderService
{
    public async Task<(OrderResponseDto? Data, string? Error)> PlaceOrderAsync(
        int customerId,
        PlaceOrderDto dto)
    {
        if (customerId <= 0)
            return (null, "Invalid customer.");

        if (string.IsNullOrWhiteSpace(dto.DeliveryAddress))
            return (null, "Delivery address is required.");

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var cart = await context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.MenuItem)
                        .ThenInclude(m => m.Restaurant)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null || cart.CartItems.Count == 0)
                return (null, "Your cart is empty.");

            if (cart.CartItems.Any(i => i.Quantity <= 0))
                return (null, "Cart contains an invalid quantity.");

            if (cart.CartItems.Any(i =>
                    !i.MenuItem.IsAvailable ||
                    !i.MenuItem.Restaurant.IsActive))
            {
                return (null,
                    "One or more items or their restaurant are unavailable.");
            }

            var restaurantIds = cart.CartItems
                .Select(i => i.MenuItem.RestaurantId)
                .Distinct()
                .ToList();

            if (restaurantIds.Count != 1)
                return (null,
                    "Cart must contain items from one restaurant.");

            var restaurantId = restaurantIds[0];

            var subtotal = cart.CartItems.Sum(
                i => i.MenuItem.Price * i.Quantity);

            if (subtotal <= 0)
                return (null, "Order subtotal must be positive.");

            const decimal deliveryFee = 50m;
            decimal discount = 0m;
            int? couponId = null;

            Coupon? coupon = null;

            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var code = dto.CouponCode.Trim().ToUpperInvariant();
                var now = DateTime.UtcNow;

                coupon = await context.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code == code &&
                        c.IsActive &&
                        c.ExpiresAt > now &&
                        c.UsedCount < c.UsageLimit);

                if (coupon == null)
                {
                    return (null,
                        "Coupon is invalid, expired, or fully used.");
                }

                if (subtotal < coupon.MinimumOrderAmount)
                {
                    return (null,
                        $"Minimum order amount for coupon is {coupon.MinimumOrderAmount}.");
                }

                if (coupon.DiscountPercent <= 0 ||
                    coupon.DiscountPercent > 100)
                {
                    return (null,
                        "Coupon discount percentage is invalid.");
                }

                couponId = coupon.CouponId;

                discount = Math.Round(
                    subtotal * coupon.DiscountPercent / 100m,
                    2,
                    MidpointRounding.AwayFromZero);

                coupon.UsedCount++;
            }

            var nowUtc = DateTime.UtcNow;

            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = restaurantId,
                CouponId = couponId,
                DeliveryAddress = dto.DeliveryAddress.Trim(),
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                DiscountAmount = discount,
                TotalAmount = subtotal + deliveryFee - discount,
                Status = "Pending",
                CreatedAt = nowUtc
            };

            foreach (var item in cart.CartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    ItemName = item.MenuItem.Name,
                    UnitPrice = item.MenuItem.Price,
                    Quantity = item.Quantity
                });
            }

            context.Orders.Add(order);

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Order = order,
                OldStatus = null,
                NewStatus = "Pending",
                ChangedBy = customerId,
                ChangedAt = nowUtc
            });

            context.CartItems.RemoveRange(cart.CartItems);

            cart.UpdatedAt = nowUtc;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await context.Entry(order)
                .Reference(o => o.Restaurant)
                .LoadAsync();

            await context.Entry(order)
                .Collection(o => o.OrderItems)
                .LoadAsync();

            return (mapper.Map<OrderResponseDto>(order), null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderResponseDto>> GetMyOrdersAsync(
        int customerId)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId)
            .ToListAsync();

        return mapper.Map<List<OrderResponseDto>>(orders);
    }

    public async Task<OrderResponseDto?> GetOrderAsync(
        int customerId,
        int orderId)
    {
        if (customerId <= 0 || orderId <= 0)
            return null;

        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o =>
                o.OrderId == orderId &&
                o.CustomerId == customerId);

        return order == null
            ? null
            : mapper.Map<OrderResponseDto>(order);
    }
}
