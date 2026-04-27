using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Identity.API.Middlewares;

public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IMemoryCache memoryCache, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress;

        if (ipAddress == null)
        {
            await next(context);
            return;
        }

        var cacheKey = $"RateLimit_{ipAddress}";

        memoryCache.TryGetValue(cacheKey, out int requestCount);

        var maxRequestCount = configuration.GetValue<int>("RateLimitSettings:MaxRequests");
        var timeWindowInSeconds = configuration.GetValue<int>("RateLimitSettings:TimeWindowInSeconds");

        if(requestCount >= maxRequestCount)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Message = "You have send too many request, please try again."
            };

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        requestCount++;

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(timeWindowInSeconds));

        memoryCache.Set(cacheKey, requestCount, cacheEntryOptions);

        await next(context);
    }
}
