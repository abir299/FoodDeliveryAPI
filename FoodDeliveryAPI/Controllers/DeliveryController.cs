using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "DeliveryPerson")]
public class DeliveryController(FoodDeliveryDbContext context)
    : ControllerBase
{
    private bool TryGetDeliveryPersonId(out int deliveryPersonId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out deliveryPersonId);
    }

    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        if (!TryGetDeliveryPersonId(out var deliveryPersonId))
            return Unauthorized();

        var assignments = await context.DeliveryAssignments
            .AsNoTracking()
            .Where(a => a.DeliveryPersonId == deliveryPersonId)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new
            {
                a.AssignmentId,
                a.OrderId,
                a.AssignedAt,
                a.DeliveredAt,
                a.Order.Status,
                a.Order.DeliveryAddress,
                a.Order.TotalAmount,
                RestaurantName = a.Order.Restaurant.Name
            })
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpPut("{assignmentId:int}/delivered")]
    public async Task<IActionResult> MarkDelivered(int assignmentId)
    {
        if (assignmentId <= 0)
            return BadRequest("Invalid assignment ID.");

        if (!TryGetDeliveryPersonId(out var deliveryPersonId))
            return Unauthorized();

        var assignment = await context.DeliveryAssignments
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a =>
                a.AssignmentId == assignmentId &&
                a.DeliveryPersonId == deliveryPersonId);

        if (assignment == null)
            return NotFound("Delivery assignment not found.");

        var oldStatus = assignment.Order.Status.Trim();

        if (oldStatus != "OutForDelivery")
            return BadRequest("Order is not out for delivery.");

        var deliveredAt = DateTime.UtcNow;

        assignment.Order.Status = "Delivered";
        assignment.DeliveredAt = deliveredAt;

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = assignment.OrderId,
            OldStatus = oldStatus,
            NewStatus = "Delivered",
            ChangedBy = deliveryPersonId,
            ChangedAt = deliveredAt
        });

        await context.SaveChangesAsync();

        return Ok(new
        {
            assignment.AssignmentId,
            assignment.OrderId,
            Status = assignment.Order.Status,
            assignment.DeliveredAt
        });
    }
}
