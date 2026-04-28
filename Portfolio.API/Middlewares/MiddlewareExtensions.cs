namespace Portfolio.API.Middlewares;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotencyMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
