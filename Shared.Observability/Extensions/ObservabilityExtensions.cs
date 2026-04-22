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
                       .AddSource("MassTransit") 
                       .AddOtlpExporter(opts =>
                       {
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
