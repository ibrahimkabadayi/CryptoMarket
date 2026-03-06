using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.CoinId)
            .IsRequired();

        builder.Property(x => x.WalletId)
            .IsRequired();
    }
}
