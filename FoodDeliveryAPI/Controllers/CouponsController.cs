
using FoodDeliveryAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController(FoodDeliveryDbContext context) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) ||
            dto.DiscountPercent <= 0 ||
            dto.DiscountPercent > 100 ||
            dto.MinimumOrderAmount < 0 ||
            dto.UsageLimit <= 0 ||
            dto.ExpiresAt <= DateTime.UtcNow)
            return BadRequest("Invalid coupon details.");

        var code = dto.Code.Trim().ToUpperInvariant();

        if (await context.Coupons.AnyAsync(c => c.Code == code))
            return Conflict("Coupon code already exists.");

        var coupon = new Data.Entities.Coupon
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

        return Ok(new
        {
            coupon.CouponId,
            coupon.Code,
            coupon.DiscountPercent,
            coupon.MinimumOrderAmount,
            coupon.UsageLimit,
            coupon.ExpiresAt
        });
    }

    [HttpGet("validate/{code}")]
    public async Task<IActionResult> Validate(string code, decimal orderAmount)
    {
        if (orderAmount <= 0)
            return BadRequest("Order amount must be positive.");

        var coupon = await context.Coupons.FirstOrDefaultAsync(c =>
            c.Code == code.Trim().ToUpper() &&
            c.IsActive &&
            c.ExpiresAt > DateTime.UtcNow &&
            c.UsedCount < c.UsageLimit);

        if (coupon == null)
            return NotFound("Coupon is invalid, expired, or fully used.");

        if (orderAmount < coupon.MinimumOrderAmount)
            return BadRequest(
                $"Minimum order amount is {coupon.MinimumOrderAmount}.");

        var discount = Math.Round(
            orderAmount * coupon.DiscountPercent / 100m, 2);

        return Ok(new
        {
            coupon.Code,
            coupon.DiscountPercent,
            DiscountAmount = discount,
            FinalAmount = orderAmount - discount
        });
    }

    public record CreateCouponDto(
        string Code,
        decimal DiscountPercent,
        decimal MinimumOrderAmount,
        int UsageLimit,
        DateTime ExpiresAt);
}
