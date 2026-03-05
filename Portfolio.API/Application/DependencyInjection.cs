using Microsoft.EntityFrameworkCore;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default Connection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        //services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        //services.AddScoped<IUserService, UserService>();

        return services;
    }
}
