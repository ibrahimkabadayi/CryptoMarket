using Shared.Observability.Extensions;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Notifications.API.Application;
using Notifications.API.Consumers;
using Notifications.API.Infrastructure;

namespace Notifications.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceTracing("Notifications.API");

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        builder.Services.AddMassTransit(configuration =>
        {
            configuration.AddConsumer<AssetTransferConsumer>();
            configuration.AddConsumer<LimitOrderConsumer>();
            configuration.AddConsumer<CoinPriceConsumer>();
            configuration.AddConsumer<UserCreatedConsumer>();

            configuration.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUsername);
                    h.Password(rabbitPassword);
                });

                cfg.ReceiveEndpoint("notification-asset-transferred-queue", e =>
                    e.ConfigureConsumer<AssetTransferConsumer>(context));

                cfg.ReceiveEndpoint("notification-limit-order-queue", e =>
                    e.ConfigureConsumer<LimitOrderConsumer>(context));

                cfg.ReceiveEndpoint("notification-coin-price-queue", e =>
                    e.ConfigureConsumer<CoinPriceConsumer>(context));

                cfg.ReceiveEndpoint("notification-user-created-queue", e =>
                    e.ConfigureConsumer<UserCreatedConsumer>(context));
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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
