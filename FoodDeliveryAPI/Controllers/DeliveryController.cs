
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
public class DeliveryController(FoodDeliveryDbContext context) : ControllerBase
{
    private int DeliveryPersonId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var assignments = await context.DeliveryAssignments
            .Where(a => a.DeliveryPersonId == DeliveryPersonId)
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
        var assignment = await context.DeliveryAssignments
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a =>
                a.AssignmentId == assignmentId &&
                a.DeliveryPersonId == DeliveryPersonId);

        if (assignment == null)
            return NotFound("Delivery assignment not found.");

        if (assignment.Order.Status.Trim() != "OutForDelivery")
            return BadRequest("Order is not out for delivery.");

        var userId = DeliveryPersonId;
        var oldStatus = assignment.Order.Status.Trim();

        assignment.Order.Status = "Delivered";
        assignment.DeliveredAt = DateTime.UtcNow;

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = assignment.OrderId,
            OldStatus = oldStatus,
            NewStatus = "Delivered",
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
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
