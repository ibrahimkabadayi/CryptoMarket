using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Shared.Infrastructure.Middlewares;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeaderKey = "X-Correlation-ID";
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = string.Empty;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out StringValues values))
        {
            correlationId = values.FirstOrDefault() ?? Guid.NewGuid().ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Append(CorrelationIdHeaderKey, correlationId);
        }

        context.Items["CorrelationId"] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderKey))
            {
                context.Response.Headers.Append(CorrelationIdHeaderKey, correlationId);
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}
