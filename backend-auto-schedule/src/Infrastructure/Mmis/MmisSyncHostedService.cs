using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Mmis;

/// <summary>
/// Фоновая служба: при старте (если включено) делает один прогон синхронизации, затем
/// запускает её ежедневно в заданное время <see cref="MmisSyncOptions.SyncAtTime"/>
/// (или каждые N минут, если задан <see cref="MmisSyncOptions.SyncIntervalMinutes"/>).
/// Каждый прогон выполняется в собственном DI-scope; сбой одного прогона не останавливает цикл.
/// </summary>
public sealed class MmisSyncHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MmisSyncOptions> options,
    ILogger<MmisSyncHostedService> logger) : BackgroundService
{
    private readonly MmisSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            logger.LogInformation("MMIS-синхронизация отключена (Enabled=false или пустая строка подключения).");
            return;
        }

        if (_options.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = NextDelay();
            logger.LogInformation("Следующая MMIS-синхронизация через {Delay} (в {At}).", delay, DateTimeOffset.Now + delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<IMmisSyncService>();
            await sync.SyncAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Остановка приложения — нормальное завершение.
        }
        catch (Exception ex)
        {
            // Уже залогировано в сервисе; здесь гарантируем, что цикл переживёт сбой.
            logger.LogWarning(ex, "Прогон MMIS-синхронизации не удался, попробуем в следующий раз.");
        }
    }

    /// <summary>Задержка до следующего прогона: фиксированный интервал или ближайшее время суток.</summary>
    private TimeSpan NextDelay()
    {
        if (_options.SyncIntervalMinutes is > 0)
            return TimeSpan.FromMinutes(_options.SyncIntervalMinutes.Value);

        var now = DateTime.Now;
        var time = TimeOnly.TryParse(_options.SyncAtTime, out var parsed) ? parsed : new TimeOnly(3, 0);
        var next = now.Date + time.ToTimeSpan();
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
