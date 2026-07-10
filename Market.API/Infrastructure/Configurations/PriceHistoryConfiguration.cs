using Market.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.OpenPrice)
            .HasPrecision(18, 8);

        builder.Property(p => p.ClosePrice)
            .HasPrecision(18, 8);

        builder.Property(p => p.HighPrice)
            .HasPrecision(18, 8);

        builder.Property(p => p.LowPrice)
            .HasPrecision(18, 8);

        builder.Property(p => p.Volume)
            .HasPrecision(20, 8);

        builder.Property(p => p.Timestamp)
            .IsRequired();

        builder.HasIndex(p => new { p.Symbol, p.Timestamp })
            .IsUnique();
    }
}
