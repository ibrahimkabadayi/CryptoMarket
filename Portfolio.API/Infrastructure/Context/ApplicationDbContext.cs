using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Infrastructure.Configurations;

namespace Portfolio.API.Infrastructure.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    DbSet<Wallet> Wallets { get; set; }
    DbSet<Transaction> Assets { get; set; }
    DbSet<Transaction> Transacitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AssetConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
    }
}
