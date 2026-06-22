using Application.Common.Generation;
using Application.Common.Interfaces;
using Application.Solver.Solving;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Mmis;
using Infrastructure.Repositories;
using Infrastructure.Schedule;
using Infrastructure.Schedule.Generation;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // Транзакции уровня SERIALIZABLE для операций записи расписания (защита от коллизий
        // при одновременной работе нескольких сотрудников бюро). См. ITransactionRunner.
        services.AddScoped<ITransactionRunner, TransactionRunner>();

        // Параметры солвера из конфигурации (секция "Solver"): лимит времени, число воркеров, лог.
        // Хендлеры получают готовое значение SolverOptions (Application не зависит от Options/Configuration).
        services.Configure<SolverOptions>(configuration.GetSection("Solver"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SolverOptions>>().Value);

        // Фоновая генерация расписания: очередь (синглтон) + служба-консьюмер.
        // HTTP-поток лишь ставит задачу в очередь, а солвер (до 180 с) работает в фоне.
        services.AddSingleton<ScheduleGenerationQueue>();
        services.AddSingleton<IScheduleGenerationQueue>(sp => sp.GetRequiredService<ScheduleGenerationQueue>());
        services.AddHostedService<ScheduleGenerationHostedService>();

        // Сидер инфраструктуры: аудиторный фонд + календарная сетка (оси «аудитории»/«время»),
        // которых нет в ММИС. Прогон при старте + после каждого ММИС-синка (новые недели).
        services.Configure<InfrastructureSeedOptions>(configuration.GetSection(InfrastructureSeedOptions.SectionName));
        services.AddScoped<IInfrastructureSeeder, InfrastructureSeeder>();
        services.AddHostedService<InfrastructureSeedHostedService>();

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