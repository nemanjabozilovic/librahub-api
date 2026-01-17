using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LibraHub.BuildingBlocks.Idempotency;

public class IdempotencyKeyMiddleware(RequestDelegate next, ILogger<IdempotencyKeyMiddleware> logger)
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore idempotencyStore)
    {
        if (context.Request.Method != "GET" && context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out Microsoft.Extensions.Primitives.StringValues value))
        {
            var idempotencyKey = value.ToString();

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existingResponse = await idempotencyStore.GetResponseAsync(idempotencyKey, context.RequestAborted);
                if (existingResponse != null)
                {
                    logger.LogInformation("Returning cached response for idempotency key {IdempotencyKey}", idempotencyKey);
                    context.Response.StatusCode = existingResponse.StatusCode;
                    context.Response.ContentType = existingResponse.ContentType;
                    await context.Response.Body.WriteAsync(existingResponse.Body, context.RequestAborted);
                    return;
                }

                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await next(context);

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync(context.RequestAborted);
                    var responseBytes = Encoding.UTF8.GetBytes(responseBodyText);

                    await idempotencyStore.StoreResponseAsync(
                        idempotencyKey,
                        context.Response.StatusCode,
                        context.Response.ContentType ?? "application/json",
                        responseBytes,
                        context.RequestAborted);

                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
                }
                else
                {
                    await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
                }

                context.Response.Body = originalBodyStream;
                return;
            }
        }

        await next(context);
    }
}
