using Market.API.Domain.Entities;
using Market.API.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Coin> Coins { get; set; }
    public DbSet<MarketNews> MarketNews { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CoinConfiguration());
        modelBuilder.ApplyConfiguration(new PriceHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new MarketNewsConfiguration());
    }
}

