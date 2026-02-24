using Identity.API.Application.Mappings;
using Identity.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Identity.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default Connection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        //services.AddScoped<>


        Console.WriteLine("\n\n---------Dependency Injection for Application has been loaded!------------\n\n");

        return services;
    }
}