namespace Identity.API.Middlewares;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app) 
    { 
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}


