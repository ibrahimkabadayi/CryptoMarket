using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Infrastructure.Configurations;

public class TreasuryBalanceConfiguration : IEntityTypeConfiguration<TreasuryBalance>
{
    public void Configure(EntityTypeBuilder<TreasuryBalance> builder)
    {
        throw new NotImplementedException();
    }
}
