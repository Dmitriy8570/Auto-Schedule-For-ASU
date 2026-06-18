using Application.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Schedule;
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
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUniversityRepository, UniversityRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IWorkloadLogRepository, WorkloadLogRepository>();
        services.AddScoped<IScheduleDataProvider, ScheduleDataProvider>();

        return services;
    }
}