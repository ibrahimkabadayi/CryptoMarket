using Identity.API.Domain.Interfaces;
using Identity.API.Infrastructure.Context;
using Identity.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default Connection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}