using Application.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
          this IServiceCollection services,
          IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUniversityRepository, UniversityRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();

        return services;
    }
}