using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController(FoodDeliveryDbContext context)
    : ControllerBase
{
    public record CreateCouponDto(
        string Code,
        decimal DiscountPercent,
        decimal MinimumOrderAmount,
        int UsageLimit,
        DateTime ExpiresAt);

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest("Coupon code is required.");

        var code = dto.Code.Trim().ToUpperInvariant();

        if (code.Length > 50)
            return BadRequest("Coupon code cannot exceed 50 characters.");

        if (dto.DiscountPercent <= 0 || dto.DiscountPercent > 100)
            return BadRequest(
                "Discount must be greater than 0 and at most 100.");

        if (dto.MinimumOrderAmount < 0)
            return BadRequest(
                "Minimum order amount cannot be negative.");

        if (dto.UsageLimit <= 0)
            return BadRequest("Usage limit must be positive.");

        if (dto.ExpiresAt <= DateTime.UtcNow)
            return BadRequest("Expiry date must be in the future.");

        if (await context.Coupons.AnyAsync(c => c.Code == code))
            return Conflict("Coupon code already exists.");

        var coupon = new Coupon
        {
            Code = code,
            DiscountPercent = dto.DiscountPercent,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            UsageLimit = dto.UsageLimit,
            UsedCount = 0,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true
        };

        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Validate),
            new
            {
                code = coupon.Code,
                orderAmount = coupon.MinimumOrderAmount
            },
            new
            {
                coupon.CouponId,
                coupon.Code,
                coupon.DiscountPercent,
                coupon.MinimumOrderAmount,
                coupon.UsageLimit,
                coupon.UsedCount,
                coupon.ExpiresAt,
                coupon.IsActive
            });
    }

    [AllowAnonymous]
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> Validate(
        string code,
        [FromQuery] decimal orderAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Coupon code is required.");

        if (orderAmount <= 0)
            return BadRequest("Order amount must be positive.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var coupon = await context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Code == normalizedCode &&
                c.IsActive &&
                c.ExpiresAt > now &&
                c.UsedCount < c.UsageLimit);

        if (coupon == null)
        {
            return NotFound(
                "Coupon is invalid, expired, or fully used.");
        }

        if (orderAmount < coupon.MinimumOrderAmount)
        {
            return BadRequest(
                $"Minimum order amount is {coupon.MinimumOrderAmount}.");
        }

        var discount = Math.Round(
            orderAmount * coupon.DiscountPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return Ok(new
        {
            coupon.Code,
            coupon.DiscountPercent,
            DiscountAmount = discount,
            FinalAmount = orderAmount - discount
        });
    }
}
