
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class OrdersController(FoodDeliveryDbContext context) : ControllerBase
{
    private int CustomerId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record PlaceOrderDto(string DeliveryAddress, string? CouponCode);

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeliveryAddress))
            return BadRequest("Delivery address is required.");

        var cart = await context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(c => c.CustomerId == CustomerId);

        if (cart == null || cart.CartItems.Count == 0)
            return BadRequest("Your cart is empty.");

        if (cart.CartItems.Any(i => !i.MenuItem.IsAvailable))
            return BadRequest("One or more items are unavailable.");

        var restaurantIds = cart.CartItems
            .Select(i => i.MenuItem.RestaurantId)
            .Distinct()
            .ToList();

        if (restaurantIds.Count != 1)
            return BadRequest("Cart must contain items from one restaurant.");

        var restaurantId = restaurantIds[0];

        var restaurantActive = await context.Restaurants.AnyAsync(
            r => r.RestaurantId == restaurantId && r.IsActive);

        if (!restaurantActive)
            return BadRequest("Restaurant is not active.");

        var subtotal = cart.CartItems.Sum(
            i => i.MenuItem.Price * i.Quantity);

        const decimal deliveryFee = 50m;
        decimal discount = 0;
        int? couponId = null;

        // Validate coupon again on the server during checkout.
        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var code = dto.CouponCode.Trim().ToUpperInvariant();

            var coupon = await context.Coupons.FirstOrDefaultAsync(c =>
                c.Code == code &&
                c.IsActive &&
                c.ExpiresAt > DateTime.UtcNow &&
                c.UsedCount < c.UsageLimit);

            if (coupon == null)
                return BadRequest("Coupon is invalid, expired, or fully used.");

            if (subtotal < coupon.MinimumOrderAmount)
                return BadRequest(
                    $"Minimum order amount for coupon is {coupon.MinimumOrderAmount}.");

            couponId = coupon.CouponId;
            discount = Math.Round(
                subtotal * coupon.DiscountPercent / 100m, 2);

            coupon.UsedCount++;
        }

        var order = new Order
        {
            CustomerId = CustomerId,
            RestaurantId = restaurantId,
            CouponId = couponId,
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            DiscountAmount = discount,
            TotalAmount = subtotal + deliveryFee - discount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
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

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        try
        {
            context.Orders.Add(order);
            context.CartItems.RemoveRange(cart.CartItems);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            order.OrderId,
            order.Status,
            order.Subtotal,
            order.DeliveryFee,
            order.DiscountAmount,
            order.TotalAmount,
            order.CreatedAt
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var orders = await context.Orders
            .Where(o => o.CustomerId == CustomerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderId,
                RestaurantName = o.Restaurant.Name,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                Items = o.OrderItems.Select(i => new
                {
                    i.ItemName,
                    i.UnitPrice,
                    i.Quantity
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await context.Orders
            .Where(o => o.OrderId == id && o.CustomerId == CustomerId)
            .Select(o => new
            {
                o.OrderId,
                RestaurantName = o.Restaurant.Name,
                o.DeliveryAddress,
                o.Status,
                o.Subtotal,
                o.DeliveryFee,
                o.DiscountAmount,
                o.TotalAmount,
                o.CreatedAt,
                Items = o.OrderItems.Select(i => new
                {
                    i.ItemName,
                    i.UnitPrice,
                    i.Quantity
                })
            })
            .FirstOrDefaultAsync();

        return order == null ? NotFound() : Ok(order);
    }
}
