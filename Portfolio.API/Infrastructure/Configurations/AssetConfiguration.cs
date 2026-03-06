using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.WalletId)
            .IsRequired();
    }
}
