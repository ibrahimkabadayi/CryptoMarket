using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Infrastructure.Configurations;

namespace Portfolio.API.Infrastructure.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Assets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<LimitOrder> LimitOrders { get; set; }
    public DbSet<TreasuryBalance> TreasuryBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AssetConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new LimitOrderConfiguration());
        modelBuilder.ApplyConfiguration(new TreasuryBalanceConfiguration());
    }
}
