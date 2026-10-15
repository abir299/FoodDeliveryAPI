using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class OrderStatusController(FoodDeliveryDbContext context)
    : ControllerBase
{
    public record UpdateStatusDto(string Status);

    [HttpPut("{orderId:int}")]
    public async Task<IActionResult> UpdateStatus(
        int orderId, UpdateStatusDto dto)
    {
        if (orderId <= 0)
            return BadRequest("Invalid order ID.");

        if (string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest("Status is required.");

        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return Unauthorized();
        }

        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound("Order not found.");

        var current = order.Status.Trim();
        var requested = dto.Status.Trim();

        var allowed = current switch
        {
            "Pending" => new[] { "Confirmed", "Cancelled" },
            "Confirmed" => new[] { "Preparing", "Cancelled" },
            "Preparing" => new[] { "Ready", "Cancelled" },
            "Ready" => new[] { "OutForDelivery", "Cancelled" },
            _ => Array.Empty<string>()
        };

        var next = allowed.FirstOrDefault(s =>
            string.Equals(
                s, requested, StringComparison.OrdinalIgnoreCase));

        if (next == null)
        {
            return BadRequest(
                $"Cannot change order from {current} to {requested}.");
        }

        order.Status = next;

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            OldStatus = current,
            NewStatus = next,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        return Ok(new
        {
            order.OrderId,
            OldStatus = current,
            order.Status
        });
    }

    [HttpGet("{orderId:int}/history")]
    public async Task<IActionResult> GetHistory(int orderId)
    {
        if (orderId <= 0)
            return BadRequest("Invalid order ID.");

        var orderExists = await context.Orders
            .AnyAsync(o => o.OrderId == orderId);

        if (!orderExists)
            return NotFound("Order not found.");

        var history = await context.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.HistoryId)
            .Select(h => new
            {
                h.HistoryId,
                h.OldStatus,
                h.NewStatus,
                h.ChangedBy,
                h.ChangedAt
            })
            .ToListAsync();

        return Ok(history);
    }
}
