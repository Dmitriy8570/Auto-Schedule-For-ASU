using Application.Common.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Mmis;
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
        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<IScheduleLookupRepository, ScheduleLookupRepository>();
        services.AddScoped<IManagementRepository, ManagementRepository>();
        services.AddScoped<IConstraintConfigurationRepository, ConstraintConfigurationRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IWorkloadRepository, WorkloadRepository>();
        services.AddScoped<IWorkloadLogRepository, WorkloadLogRepository>();
        services.AddScoped<IScheduleDataProvider, ScheduleDataProvider>();

        // Синхронизация с MMIS (MS SQL): фоновая ежедневная выгрузка + журналирование нагрузки.
        services.Configure<MmisSyncOptions>(configuration.GetSection(MmisSyncOptions.SectionName));
        services.AddSingleton<MmisSyncStatus>();
        services.AddScoped<MmisReader>();
        services.AddScoped<IMmisSyncService, MmisSyncService>();
        services.AddHostedService<MmisSyncHostedService>();

        // Аутентификация: LDAP-bind в Active Directory + выпуск JWT.
        services.Configure<AdAuthOptions>(configuration.GetSection(AdAuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IAuthenticationService, LdapAuthenticationService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}