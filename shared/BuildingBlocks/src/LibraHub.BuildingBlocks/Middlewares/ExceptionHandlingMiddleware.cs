using LibraHub.BuildingBlocks.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}, StatusCode: {StatusCode}",
                context.Request.Path,
                context.Request.Method,
                context.Response.StatusCode);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var error = new Error("INTERNAL_ERROR", "An internal error occurred");
        var response = new { code = error.Code, message = error.Message };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
