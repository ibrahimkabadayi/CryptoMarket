using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Mappings;
using Portfolio.API.Application.Services;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Caching;
using Portfolio.API.Infrastructure.Context;
using Portfolio.API.Infrastructure.Repositories;
using Portfolio.API.Hubs;
using Testcontainers.PostgreSql;

namespace CryptoMarket.IntegrationTests.TestContainers;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public PostgresFixture()
    {
    }

    public ServiceProvider Provider { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging();

        // Gerçek veritabanı
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseNpgsql(_container.GetConnectionString()));     

        // Test edilen gerçek kod
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddAutoMapper(cfg => cfg.AddProfile<WalletMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<AssetMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<TransactionMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<LimitOrderMapping>());
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IWalletService, WalletService>();
        // + WalletService'in ihtiyaç duyduğu repository kayıtları (Program.cs'teki gibi)

        // Sahte bağımlılıklar
        services.RemoveAll<IPublishEndpoint>();
        services.RemoveAll<ICacheService>();

        services.AddSingleton(Substitute.For<IPublishEndpoint>());
        services.AddSingleton(Substitute.For<ICacheService>());

        var hubContext = Substitute.For<IHubContext<PortfolioHub>>();
        var hubClients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(hubClients);
        hubContext.Groups.Returns(Substitute.For<IGroupManager>());
        hubClients.User(Arg.Any<string>()).Returns(clientProxy);
        services.AddSingleton(hubContext);

        Provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true   // eksik kayıtları test başında yakalar
        });

        // Gerçek migration'larla şemayı kur (CHECK constraint ve indeksler dahil)
        await using var scope = Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await _container.DisposeAsync();
    }
}
