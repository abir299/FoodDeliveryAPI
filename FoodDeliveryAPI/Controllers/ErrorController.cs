using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
public class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult HandleError()
    {
        var exception = HttpContext.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        return Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.",
            detail: "Please try again later.");
    }
}
