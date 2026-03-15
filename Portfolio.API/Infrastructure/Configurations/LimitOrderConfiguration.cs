using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class LimitOrderConfiguration : IEntityTypeConfiguration<LimitOrder>
{
    public void Configure(EntityTypeBuilder<LimitOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(X => X.TargetPrice)
            .IsRequired();

        builder.HasOne(x => x.Wallet)
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
    }
}
