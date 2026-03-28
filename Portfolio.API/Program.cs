
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Portfolio.API.Application;
using Portfolio.API.Consumers;
using Portfolio.API.Infrastructure;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

        builder.Services.AddMassTransit(configuration =>
        {
            configuration.AddConsumer<UserCreatedConsumer>();
            configuration.AddConsumer<CoinPriceConsumer>();

            configuration.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("portfolio-user-created-queue", e =>
                {
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                    e.ConfigureConsumer<CoinPriceConsumer>(context);
                });
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
