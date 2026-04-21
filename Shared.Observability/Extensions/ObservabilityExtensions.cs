using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Observability.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddServiceTracing(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddSource("MassTransit") // MassTransit uses this ActivitySource for its traces
                       .AddOtlpExporter(opts =>
                       {
                           // We will define this environment variable in docker-compose.yml
                           // Usually: http://jaeger:4317
                           var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                           if (!string.IsNullOrEmpty(endpoint))
                           {
                               opts.Endpoint = new Uri(endpoint);
                           }
                       });
            });

        return builder;
    }
}
