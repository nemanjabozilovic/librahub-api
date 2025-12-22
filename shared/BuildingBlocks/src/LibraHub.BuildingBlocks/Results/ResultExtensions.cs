using Microsoft.AspNetCore.Mvc;

namespace LibraHub.BuildingBlocks.Results;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return result.Error!.Code switch
        {
            "NOT_FOUND" => controller.NotFound(new { code = result.Error.Code, message = result.Error.Message }),
            "UNAUTHORIZED" => controller.Unauthorized(new { code = result.Error.Code, message = result.Error.Message }),
            "FORBIDDEN" => controller.StatusCode(403, new { code = result.Error.Code, message = result.Error.Message }),
            "CONFLICT" => controller.Conflict(new { code = result.Error.Code, message = result.Error.Message }),
            _ => controller.BadRequest(new { code = result.Error.Code, message = result.Error.Message })
        };
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok();
        }

        return result.Error!.Code switch
        {
            "NOT_FOUND" => controller.NotFound(new { code = result.Error.Code, message = result.Error.Message }),
            "UNAUTHORIZED" => controller.Unauthorized(new { code = result.Error.Code, message = result.Error.Message }),
            "FORBIDDEN" => controller.StatusCode(403, new { code = result.Error.Code, message = result.Error.Message }),
            "CONFLICT" => controller.Conflict(new { code = result.Error.Code, message = result.Error.Message }),
            _ => controller.BadRequest(new { code = result.Error.Code, message = result.Error.Message })
        };
    }

    public static IActionResult ToCreatedActionResult<T>(this Result<T> result, ControllerBase controller, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            return controller.CreatedAtAction(actionName, routeValues, result.Value);
        }

        return result.Error!.Code switch
        {
            "NOT_FOUND" => controller.NotFound(new { code = result.Error.Code, message = result.Error.Message }),
            "UNAUTHORIZED" => controller.Unauthorized(new { code = result.Error.Code, message = result.Error.Message }),
            "FORBIDDEN" => controller.StatusCode(403, new { code = result.Error.Code, message = result.Error.Message }),
            "CONFLICT" => controller.Conflict(new { code = result.Error.Code, message = result.Error.Message }),
            _ => controller.BadRequest(new { code = result.Error.Code, message = result.Error.Message })
        };
    }

    public static IActionResult ToNoContentActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return result.Error!.Code switch
        {
            "NOT_FOUND" => controller.NotFound(new { code = result.Error.Code, message = result.Error.Message }),
            "UNAUTHORIZED" => controller.Unauthorized(new { code = result.Error.Code, message = result.Error.Message }),
            "FORBIDDEN" => controller.StatusCode(403, new { code = result.Error.Code, message = result.Error.Message }),
            "CONFLICT" => controller.Conflict(new { code = result.Error.Code, message = result.Error.Message }),
            _ => controller.BadRequest(new { code = result.Error.Code, message = result.Error.Message })
        };
    }
}
