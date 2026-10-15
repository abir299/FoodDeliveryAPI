using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController(FoodDeliveryDbContext context) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetRestaurants()
    {
        var restaurants = await context.Restaurants
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new
            {
                r.RestaurantId,
                r.Name,
                r.Description,
                r.Address
            })
            .ToListAsync();

        return Ok(restaurants);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRestaurant(int id)
    {
        var restaurant = await context.Restaurants
            .AsNoTracking()
            .Where(r => r.RestaurantId == id && r.IsActive)
            .Select(r => new
            {
                r.RestaurantId,
                r.Name,
                r.Description,
                r.Address,
                Menu = r.MenuItems
                    .Where(m => m.IsAvailable)
                    .Select(m => new
                    {
                        m.MenuItemId,
                        m.Name,
                        m.Description,
                        m.Price,
                        m.CategoryId
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return restaurant == null
            ? NotFound("Restaurant not found.")
            : Ok(restaurant);
    }

    public record RestaurantDto(
        string Name,
        string? Description,
        string Address);

    [Authorize(Roles = "RestaurantOwner")]
    [HttpPost]
    public async Task<IActionResult> CreateRestaurant(RestaurantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Restaurant name and address are required.");

        var ownerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(ownerIdValue, out var ownerId))
            return Unauthorized();

        var restaurant = new Restaurant
        {
            OwnerId = ownerId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            Address = dto.Address.Trim(),
            IsActive = true
        };

        context.Restaurants.Add(restaurant);
        await context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRestaurant),
            new { id = restaurant.RestaurantId },
            new
            {
                restaurant.RestaurantId,
                restaurant.Name,
                restaurant.Description,
                restaurant.Address,
                restaurant.IsActive
            });
    }

    [Authorize(Roles = "RestaurantOwner")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRestaurant(
        int id, RestaurantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Restaurant name and address are required.");

        var ownerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(ownerIdValue, out var ownerId))
            return Unauthorized();

        var restaurant = await context.Restaurants.FirstOrDefaultAsync(
            r => r.RestaurantId == id &&
                 r.OwnerId == ownerId &&
                 r.IsActive);

        if (restaurant == null)
            return NotFound("Restaurant not found or not owned by you.");

        restaurant.Name = dto.Name.Trim();
        restaurant.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        restaurant.Address = dto.Address.Trim();

        await context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "RestaurantOwner")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        var ownerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(ownerIdValue, out var ownerId))
            return Unauthorized();

        var restaurant = await context.Restaurants.FirstOrDefaultAsync(
            r => r.RestaurantId == id &&
                 r.OwnerId == ownerId &&
                 r.IsActive);

        if (restaurant == null)
            return NotFound("Restaurant not found or not owned by you.");

        restaurant.IsActive = false;
        await context.SaveChangesAsync();

        return NoContent();
    }
}