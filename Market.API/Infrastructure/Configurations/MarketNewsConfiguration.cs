using Market.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Configurations;

public class MarketNewsConfiguration : IEntityTypeConfiguration<MarketNews>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MarketNews> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.RelatedSymbols)
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property(n => n.PublishedAt)
            .IsRequired();

        builder.HasIndex(n => n.PublishedAt);
    }
}
