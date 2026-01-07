using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LibraHub.BuildingBlocks.Results;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return HandleError(result.Error!, controller);
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok();
        }

        return HandleError(result.Error!, controller);
    }

    public static IActionResult ToCreatedActionResult<T>(this Result<T> result, ControllerBase controller, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            return controller.CreatedAtAction(actionName, routeValues, result.Value);
        }

        return HandleError(result.Error!, controller);
    }

    public static IActionResult ToNoContentActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return HandleError(result.Error!, controller);
    }

    private static IActionResult HandleError(Error error, ControllerBase controller)
    {
        var logger = controller.HttpContext.RequestServices.GetService(typeof(ILogger<ControllerBase>)) as ILogger<ControllerBase>;
        var statusCode = GetStatusCode(error.Code);

        logger?.LogWarning(
            "Request failed. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}, StatusCode: {StatusCode}, RequestPath: {RequestPath}, Method: {Method}",
            error.Code,
            error.Message,
            statusCode,
            controller.HttpContext.Request.Path,
            controller.HttpContext.Request.Method);

        var errorResponse = new { code = error.Code, message = error.Message };

        return error.Code switch
        {
            "NOT_FOUND" => controller.NotFound(errorResponse),
            "UNAUTHORIZED" => controller.Unauthorized(errorResponse),
            "FORBIDDEN" => controller.StatusCode(403, errorResponse),
            "CONFLICT" => controller.Conflict(errorResponse),
            _ => controller.BadRequest(errorResponse)
        };
    }

    private static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "NOT_FOUND" => 404,
            "UNAUTHORIZED" => 401,
            "FORBIDDEN" => 403,
            "CONFLICT" => 409,
            _ => 400
        };
    }
}
