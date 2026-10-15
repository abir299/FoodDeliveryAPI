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
public class CartsController(FoodDeliveryDbContext context) : ControllerBase
{
    private int CustomerId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record AddCartItemDto(int MenuItemId, int Quantity);

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await context.Carts
            .AsNoTracking()
            .Where(c => c.CustomerId == CustomerId)
            .Select(c => new
            {
                c.CartId,
                Items = c.CartItems.Select(i => new
                {
                    i.CartItemId,
                    i.MenuItemId,
                    i.MenuItem.Name,
                    i.MenuItem.Price,
                    i.Quantity,
                    LineTotal = i.MenuItem.Price * i.Quantity
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (cart == null)
            return Ok(new { Items = new object[0], Total = 0m });

        var total = cart.Items.Sum(i => i.LineTotal);

        return Ok(new
        {
            cart.CartId,
            cart.Items,
            Total = total
        });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var menuItem = await context.MenuItems
            .Include(m => m.Restaurant)
            .FirstOrDefaultAsync(m =>
                m.MenuItemId == dto.MenuItemId &&
                m.IsAvailable &&
                m.Restaurant.IsActive);

        if (menuItem == null)
            return NotFound("Available menu item not found.");

        var cart = await context.Carts
            .FirstOrDefaultAsync(c => c.CustomerId == CustomerId);

        if (cart == null)
        {
            cart = new Cart
            {
                CustomerId = CustomerId,
                UpdatedAt = DateTime.UtcNow
            };

            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        var existingItems = await context.CartItems
            .Where(i => i.CartId == cart.CartId)
            .Include(i => i.MenuItem)
            .ToListAsync();

        if (existingItems.Any(i =>
                i.MenuItem.RestaurantId != menuItem.RestaurantId))
        {
            return BadRequest(
                "Your cart can contain items from only one restaurant. Clear your cart first.");
        }

        var existing = existingItems.FirstOrDefault(
            i => i.MenuItemId == dto.MenuItemId);

        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            context.CartItems.Add(new CartItem
            {
                CartId = cart.CartId,
                MenuItemId = dto.MenuItemId,
                Quantity = dto.Quantity
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new { message = "Cart updated successfully." });
    }

    [HttpPut("items/{cartItemId:int}")]
    public async Task<IActionResult> UpdateItem(
        int cartItemId, AddCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var item = await context.CartItems
            .Include(i => i.Cart)
            .Include(i => i.MenuItem)
                .ThenInclude(m => m.Restaurant)
            .FirstOrDefaultAsync(i =>
                i.CartItemId == cartItemId &&
                i.Cart.CustomerId == CustomerId);

        if (item == null)
            return NotFound("Cart item not found.");

        if (!item.MenuItem.IsAvailable ||
            !item.MenuItem.Restaurant.IsActive)
        {
            return BadRequest(
                "This menu item is no longer available.");
        }

        item.Quantity = dto.Quantity;
        item.Cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("items/{cartItemId:int}")]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        var item = await context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.CartItemId == cartItemId &&
                i.Cart.CustomerId == CustomerId);

        if (item == null)
            return NotFound("Cart item not found.");

        var cart = item.Cart;

        context.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.CustomerId == CustomerId);

        if (cart == null)
            return NoContent();

        context.CartItems.RemoveRange(cart.CartItems);
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
}
