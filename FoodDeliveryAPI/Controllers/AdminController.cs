
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
public class AdminController(FoodDeliveryDbContext context) : ControllerBase
{
    public record AssignDeliveryDto(int DeliveryPersonId);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = new
        {
            TotalUsers = await context.Users.CountAsync(),
            TotalRestaurants = await context.Restaurants.CountAsync(),
            TotalMenuItems = await context.MenuItems.CountAsync(),
            TotalOrders = await context.Orders.CountAsync(),
            PendingOrders = await context.Orders.CountAsync(
                o => o.Status == "Pending"),
            DeliveredOrders = await context.Orders.CountAsync(
                o => o.Status == "Delivered"),
            TotalRevenue = await context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m
        };

        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders(
        int page = 1, int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination values.");

        var query = context.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                CustomerName = o.Customer.FullName,
                RestaurantName = o.Restaurant.Name,
                o.Status,
                o.TotalAmount,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, orders });
    }

    [HttpPost("orders/{orderId:int}/assign-delivery")]
    public async Task<IActionResult> AssignDelivery(
        int orderId, AssignDeliveryDto dto)
    {
        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound("Order not found.");

        if (order.Status.Trim() != "Ready")
            return BadRequest("Only Ready orders can be assigned.");

        var deliveryPerson = await context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == dto.DeliveryPersonId &&
                u.Role.Trim() == "DeliveryPerson");

        if (deliveryPerson == null)
            return BadRequest("Valid delivery person not found.");

        if (await context.DeliveryAssignments.AnyAsync(
            a => a.OrderId == orderId))
            return Conflict("Order already has a delivery assignment.");

        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var assignment = new DeliveryAssignment
        {
            OrderId = orderId,
            DeliveryPersonId = dto.DeliveryPersonId,
            AssignedAt = DateTime.UtcNow
        };

        order.Status = "OutForDelivery";
        context.DeliveryAssignments.Add(assignment);

        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            OldStatus = "Ready",
            NewStatus = "OutForDelivery",
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            assignment.AssignmentId,
            assignment.OrderId,
            assignment.DeliveryPersonId,
            assignment.AssignedAt,
            Status = order.Status
        });
    }
}
