using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Seed;

/// <summary>
/// При старте приложения один раз наполняет аудиторный фонд и достраивает календарную сетку
/// для уже имеющихся недель. Недели, появляющиеся позже при ММИС-синхронизации, получают
/// сетку из самого <c>MmisSyncService</c> (он вызывает <see cref="IInfrastructureSeeder.SeedCalendarGridAsync"/>
/// после переноса недель), поэтому фоновый цикл здесь не нужен.
/// Прогон выполняется в собственном DI-scope; сбой логируется и не роняет приложение.
/// </summary>
public sealed class InfrastructureSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<InfrastructureSeedOptions> options,
    ILogger<InfrastructureSeedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Сидер инфраструктуры отключён (Enabled=false).");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IInfrastructureSeeder>();
            await seeder.SeedFacilitiesAsync(stoppingToken);
            // Оборудование оснащает аудитории — наполняем после аудиторного фонда.
            await seeder.SeedEquipmentAsync(stoppingToken);
            await seeder.SeedConstraintWeightsAsync(stoppingToken);
            await seeder.SeedCalendarGridAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Остановка приложения — нормальное завершение.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Сидинг инфраструктуры завершился с ошибкой.");
        }
    }
}
