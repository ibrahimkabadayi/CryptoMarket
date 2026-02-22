using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    DbSet<User> Users { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Console.WriteLine("\n \n \n ------------Database Connected!------------\n \n \n ");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        Console.WriteLine("\n \n \n ------------Database Connected!------------\n \n \n ");
    }
}