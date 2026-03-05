using Identity.API.Application.Mappings;
using Identity.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Services;

namespace Identity.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}