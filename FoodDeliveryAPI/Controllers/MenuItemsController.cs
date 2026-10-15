using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController(FoodDeliveryDbContext context) : ControllerBase
{
    public record MenuItemDto(
        int CategoryId,
        string Name,
        string? Description,
        decimal Price);

    [AllowAnonymous]
    [HttpGet("restaurant/{restaurantId:int}")]
    public async Task<IActionResult> GetMenu(int restaurantId)
    {
        var exists = await context.Restaurants.AnyAsync(
            r => r.RestaurantId == restaurantId && r.IsActive);

        if (!exists)
            return NotFound("Restaurant not found.");

        var items = await context.MenuItems
            .AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && m.IsAvailable)
            .Select(m => new
            {
                m.MenuItemId,
                m.RestaurantId,
                m.CategoryId,
                CategoryName = m.Category.Name,
                m.Name,
                m.Description,
                m.Price
            })
            .ToListAsync();

        return Ok(items);
    }

    [Authorize(Roles = "RestaurantOwner")]
    [HttpPost("restaurant/{restaurantId:int}")]
    public async Task<IActionResult> Create(
        int restaurantId, MenuItemDto dto)
    {
        if (!TryGetOwnerId(out var ownerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
            return BadRequest("Name is required and price must be positive.");

        var restaurant = await context.Restaurants.FirstOrDefaultAsync(
            r => r.RestaurantId == restaurantId &&
                 r.OwnerId == ownerId &&
                 r.IsActive);

        if (restaurant == null)
            return NotFound("Restaurant not found or not owned by you.");

        var categoryExists = await context.Categories.AnyAsync(
            c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
            return BadRequest("Invalid category.");

        var item = new MenuItem
        {
            RestaurantId = restaurantId,
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            Price = dto.Price,
            IsAvailable = true
        };

        context.MenuItems.Add(item);
        await context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetMenu),
            new { restaurantId },
            new
            {
                item.MenuItemId,
                item.RestaurantId,
                item.CategoryId,
                item.Name,
                item.Description,
                item.Price,
                item.IsAvailable
            });
    }

    [Authorize(Roles = "RestaurantOwner")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MenuItemDto dto)
    {
        if (!TryGetOwnerId(out var ownerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
            return BadRequest("Name is required and price must be positive.");

        var item = await context.MenuItems
            .Include(m => m.Restaurant)
            .FirstOrDefaultAsync(m =>
                m.MenuItemId == id &&
                m.Restaurant.OwnerId == ownerId &&
                m.Restaurant.IsActive &&
                m.IsAvailable);

        if (item == null)
            return NotFound("Available menu item not found or not owned by you.");

        var categoryExists = await context.Categories.AnyAsync(
            c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
            return BadRequest("Invalid category.");

        item.CategoryId = dto.CategoryId;
        item.Name = dto.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        item.Price = dto.Price;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "RestaurantOwner")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
            return Unauthorized();

        var item = await context.MenuItems
            .Include(m => m.Restaurant)
            .FirstOrDefaultAsync(m =>
                m.MenuItemId == id &&
                m.Restaurant.OwnerId == ownerId &&
                m.Restaurant.IsActive &&
                m.IsAvailable);

        if (item == null)
            return NotFound("Available menu item not found or not owned by you.");

        item.IsAvailable = false;
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool TryGetOwnerId(out int ownerId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out ownerId);
    }
}