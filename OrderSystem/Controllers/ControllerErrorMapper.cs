using Microsoft.AspNetCore.Mvc;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

internal static class ControllerErrorMapper
{
    public static ActionResult<TResponse> ToActionResult<TResponse>(
        this ControllerBase controller,
        DomainException exception)
    {
        return exception.StatusCode switch
        {
            StatusCodes.Status400BadRequest => controller.BadRequest(exception.Message),
            StatusCodes.Status404NotFound => controller.NotFound(exception.Message),
            StatusCodes.Status409Conflict => controller.Conflict(exception.Message),
            _ => controller.Problem(statusCode: exception.StatusCode, title: exception.Message)
        };
    }
}
