using FoodDeliveryAPI.DTOs;
using FoodDeliveryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private bool TryGetCustomerId(out int customerId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out customerId);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var (data, error) = await orderService.PlaceOrderAsync(
            customerId, dto);

        if (error != null)
            return BadRequest(new { message = error });

        return Ok(data);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var orders = await orderService.GetMyOrdersAsync(customerId);

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid order ID." });

        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var order = await orderService.GetOrderAsync(customerId, id);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        return Ok(order);
    }
}