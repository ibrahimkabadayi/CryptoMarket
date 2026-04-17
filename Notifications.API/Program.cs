using MassTransit;
using Notifications.API.Application;
using Notifications.API.Consumers;
using Notifications.API.Infrastructure;

namespace Notifications.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

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
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("notification-asset-transfared-queue", e =>
                {
                    e.ConfigureConsumer<AssetTransferConsumer>(context);
                    e.ConfigureConsumer<LimitOrderConsumer>(context);
                    e.ConfigureConsumer<CoinPriceConsumer>(context);
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                });
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
