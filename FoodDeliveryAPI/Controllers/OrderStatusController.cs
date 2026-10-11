
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
public class OrderStatusController(FoodDeliveryDbContext context) : ControllerBase
{
    public record UpdateStatusDto(string Status);

    [HttpPut("{orderId:int}")]
    public async Task<IActionResult> UpdateStatus(
        int orderId, UpdateStatusDto dto)
    {
        var order = await context.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Order not found.");

        var next = dto.Status.Trim();
        var current = order.Status.Trim();

        var allowed = current switch
        {
            "Pending" => new[] { "Confirmed", "Cancelled" },
            "Confirmed" => new[] { "Preparing", "Cancelled" },
            "Preparing" => new[] { "Ready" },
            _ => Array.Empty<string>()
        };

        if (!allowed.Contains(next))
            return BadRequest($"Cannot change order from {current} to {next}.");

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        order.Status = next;

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            OldStatus = current,
            NewStatus = next,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        return Ok(new { order.OrderId, order.Status });
    }

    [HttpGet("{orderId:int}/history")]
    public async Task<IActionResult> GetHistory(int orderId)
    {
        if (!await context.Orders.AnyAsync(o => o.OrderId == orderId))
            return NotFound("Order not found.");

        var history = await context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.ChangedAt)
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
