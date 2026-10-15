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
                o => o.Status.Trim() == "Pending"),

            DeliveredOrders = await context.Orders.CountAsync(
                o => o.Status.Trim() == "Delivered"),

            TotalRevenue = await context.Orders
                .Where(o => o.Status.Trim() == "Delivered")
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

        var query = context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId);

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

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            orders
        });
    }

    [HttpPost("orders/{orderId:int}/assign-delivery")]
    public async Task<IActionResult> AssignDelivery(
        int orderId, AssignDeliveryDto dto)
    {
        if (orderId <= 0 || dto.DeliveryPersonId <= 0)
            return BadRequest("Invalid order or delivery person ID.");

        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var adminId))
        {
            return Unauthorized();
        }

        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound("Order not found.");

        var currentStatus = order.Status.Trim();

        if (currentStatus is "Delivered" or "Cancelled")
            return BadRequest(
                "Cannot assign a completed or cancelled order.");

        if (currentStatus is not ("Ready" or "OutForDelivery"))
            return BadRequest(
                "Only Ready or OutForDelivery orders can be assigned.");

        var newDeliveryPerson = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.UserId == dto.DeliveryPersonId &&
                u.Role.Trim() == "DeliveryPerson");

        if (newDeliveryPerson == null)
            return BadRequest("Invalid delivery person.");

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        try
        {
            var assignment = await context.DeliveryAssignments
                .FirstOrDefaultAsync(a => a.OrderId == orderId);

            if (assignment?.DeliveredAt != null)
                return BadRequest(
                    "This order has already been delivered.");

            var now = DateTime.UtcNow;

            if (assignment == null)
            {
                assignment = new DeliveryAssignment
                {
                    OrderId = orderId,
                    DeliveryPersonId = dto.DeliveryPersonId,
                    AssignedAt = now
                };

                context.DeliveryAssignments.Add(assignment);
            }
            else
            {
                assignment.DeliveryPersonId = dto.DeliveryPersonId;
                assignment.AssignedAt = now;
                assignment.DeliveredAt = null;
            }

            if (currentStatus == "Ready")
            {
                order.Status = "OutForDelivery";

                context.OrderStatusHistories.Add(
                    new OrderStatusHistory
                    {
                        OrderId = orderId,
                        OldStatus = currentStatus,
                        NewStatus = "OutForDelivery",
                        ChangedBy = adminId,
                        ChangedAt = now
                    });
            }

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
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
