using FoodDeliveryAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(FoodDeliveryDbContext context)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.CategoryId,
                c.Name
            })
            .ToListAsync();

        return Ok(categories);
    }
}
