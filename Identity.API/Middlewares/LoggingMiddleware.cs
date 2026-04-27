using System.Diagnostics;

namespace Identity.API.Middlewares;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        logger.LogInformation(
            $"[{context.Request.Method}] {context.Request.Path} request handled. Status Code: {context.Response.StatusCode} | Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}
