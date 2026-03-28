using Microsoft.EntityFrameworkCore;

namespace Notifications.API.Infrastructure.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    //DbSet<> 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}
