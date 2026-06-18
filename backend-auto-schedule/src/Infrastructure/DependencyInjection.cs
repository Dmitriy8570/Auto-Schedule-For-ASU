using Application.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Mmis;
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
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUniversityRepository, UniversityRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();

        // Синхронизация с MMIS (MS SQL): фоновая ежедневная выгрузка + журналирование нагрузки.
        services.Configure<MmisSyncOptions>(configuration.GetSection(MmisSyncOptions.SectionName));
        services.AddSingleton<MmisSyncStatus>();
        services.AddScoped<MmisReader>();
        services.AddScoped<IMmisSyncService, MmisSyncService>();
        services.AddHostedService<MmisSyncHostedService>();

        return services;
    }
}