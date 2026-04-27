using Shared.Observability.Extensions;
using System.Text;
using Market.API.Application;
using Market.API.Consumers;
using Market.API.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Middlewares;

namespace Market.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceTracing("Market.API");


        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "redis_db:6379";
            options.InstanceName = "MarketDb_";
        });

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h => {
                    h.Username(rabbitUsername);
                    h.Password(rabbitPassword);
                });

                cfg.ReceiveEndpoint("market-user-created-queue", e =>
                {
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                });
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("Jwt");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                };
            });

        builder.Services.AddAuthorization();


        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCorrelationIdMiddleware();

        app.UseLoggingMiddleware();

        app.UseExceptionHandlingMiddleware();

        app.UseAuthentication();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}