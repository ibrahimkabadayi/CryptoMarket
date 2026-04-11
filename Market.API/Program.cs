using Market.API.Application;
using Market.API.Consumers;
using Market.API.Infrastructure;
using MassTransit;

namespace Market.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddBybit(options =>
        {
           // options.Proxy = new ApiProxy("proxy_host", 1080); // SOCKS5/HTTP proxy
        });

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "redis_db:6379";
            options.InstanceName = "MarketDb_";
        });

        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h => {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("market-user-created-queue", e =>
                {
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