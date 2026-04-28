using System.Net;
using Microsoft.Extensions.Caching.Distributed;
using Portfolio.API.Application.Interfaces;

namespace Portfolio.API.Middlewares;

public class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICacheService cacheService)
    {
        var idempotencyHeader = "Idempotency-Key";

        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(idempotencyHeader, out var idempotencyKey))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Message = "For financial operations 'Idempotency-Key' header is mandotory."
            });
            return;
        }

        var cacheKey = $"Idemp_{idempotencyKey}";

        var existingKey = await cacheService.GetAsync<string>(cacheKey);

        if (!string.IsNullOrEmpty(existingKey))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Message = "This transaction is either pending or already done."
            });
            return;
        }

        await cacheService.SetAsync(cacheKey, idempotencyKey, TimeSpan.FromHours(24));

        await next(context);
    }
}
