using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .HasMany(x => x.Assets)
            .WithOne(x => x.Wallet)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Wallet)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(x => x.Address)
            .IsRequired();
        
        builder
            .Property(x => x.FiatBalance)
            .HasDefaultValue(0)
            .IsRequired();
    }
}
