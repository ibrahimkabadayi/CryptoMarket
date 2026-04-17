using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class TreasuryBalanceConfiguration : IEntityTypeConfiguration<TreasuryBalance>
{
    public void Configure(EntityTypeBuilder<TreasuryBalance> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.AssetSymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.TotalAmount)
            .HasColumnType("decimal(18,8)");
    }
}
