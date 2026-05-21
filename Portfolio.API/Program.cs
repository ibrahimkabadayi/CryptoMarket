using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfolio.API.Application;
using Portfolio.API.Consumers;
using Portfolio.API.Infrastructure;
using Portfolio.API.Infrastructure.Context;
using Shared.Infrastructure.Middlewares;
using Portfolio.API.Middlewares;
using Shared.Infrastructure.Extensions;
using Portfolio.API.Hubs;

namespace Portfolio.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceTracing("Portfolio.API");

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddSignalR();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowVueApp", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        builder.Services.AddMassTransit(configuration =>
        {
            configuration.AddConsumer<UserCreatedConsumer>();
            configuration.AddConsumer<CoinPriceConsumer>();
            configuration.AddConsumer<SetLimitOrderEvent>();

            configuration.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUsername);
                    h.Password(rabbitPassword);
                });

                cfg.ReceiveEndpoint("portfolio-user-created-queue", e =>
                    e.ConfigureConsumer<UserCreatedConsumer>(context));             

                cfg.ReceiveEndpoint("portfolio-coin-price-queue", e =>
                    e.ConfigureConsumer<CoinPriceConsumer>(context));

                cfg.ReceiveEndpoint("portfolio-set-limit-order-queue", e =>
                    e.ConfigureConsumer<SetLimitOrderEvent>(context));
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("Jwt");
                Console.WriteLine(jwtSettings["Key"]);
                Console.WriteLine("KEY: " + jwtSettings["Key"]);
                Console.WriteLine("ISSUER: " + jwtSettings["Issuer"]);
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

                // Allow SignalR clients to send access_token in query string for WebSockets
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                        var path = context.HttpContext.Request.Path;

                        // Buraya kontrol loglarý ekliyoruz:
                        Console.WriteLine("\n--- SIGNALR TOKEN KONTROLÜ ---");
                        Console.WriteLine($"Ýstek Yolu (Path): {path}");
                        Console.WriteLine($"URL'de Token Bulundu mu?: {!string.IsNullOrEmpty(accessToken)}");

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/portfolio"))
                        {
                            Console.WriteLine("Baþarýlý: Token içeri alýndý!");
                            context.Token = accessToken;
                        }
                        else
                        {
                            Console.WriteLine("HATA: Koþul saðlanamadý, token reddedildi!");
                        }
                        Console.WriteLine("------------------------------\n");

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddCustomHealthChecks(builder.Configuration);

        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("AllowVueApp");

        app.UseCorrelationIdMiddleware();

        app.UseLoggingMiddleware();

        app.UseExceptionHandlingMiddleware();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseIdempotencyMiddleware();

        app.MapControllers();

        app.MapHub<PortfolioHub>("hubs/portfolio");

        app.MapCustomHealthChecks();

        app.Run();
    }
}
