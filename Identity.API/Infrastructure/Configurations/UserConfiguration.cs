using Identity.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedDate)
            .IsRequired();

        builder.Property(x => x.UpdatedDate);

        Console.WriteLine(builder.Property(x => x.UpdatedDate).GetType().Name);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(128);
    }
}