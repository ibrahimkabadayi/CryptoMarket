using Market.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Configurations;

public class CoinConfiguration : IEntityTypeConfiguration<Coin>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Coin> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.CurrentPrice)
            .HasPrecision(18, 8);

        builder.Property(c => c.Supply)
            .HasPrecision(20, 8);

        builder.Property(c => c.MarketCap)
            .HasPrecision(20, 2);

        builder.Property(c => c.LastUpdated)
            .IsRequired();

        builder.HasIndex(c => c.Symbol)
            .IsUnique();
    }
}
