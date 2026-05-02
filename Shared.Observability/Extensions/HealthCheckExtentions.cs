using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Extensions;

public static class HealthCheckExtentions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckBuilder = services.AddHealthChecks();

        var postgresConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthCheckBuilder.AddNpgSql(
                postgresConnectionString,
                name: "PostgreSQL Check",
                tags: ["ready", "db", "postgresql"]);
        }

        var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"];

        if (!string.IsNullOrEmpty(mongoConnectionString))
        {
            healthCheckBuilder.AddMongoDb(
                name: "MongoDB Check",
                tags: ["ready", "db", "mongodb"]);
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthCheckBuilder.AddRedis(
                redisConnectionString,
                name: "Redis Check",
                tags: ["ready", "cache", "redis"]);
        }

        var rabbitConnectionString = configuration.GetConnectionString("RabbitMQ:Host");
        if (!string.IsNullOrEmpty(rabbitConnectionString))
        {
            healthCheckBuilder.AddRabbitMQ(
               name: "RabbitMQ Check",
               tags: ["ready", "messagebroker", "rabbitmq"]);
        }

        return services;
    }

    public static WebApplication MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
