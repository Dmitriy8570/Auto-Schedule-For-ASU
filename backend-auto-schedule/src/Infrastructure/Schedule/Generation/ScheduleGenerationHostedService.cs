using Application.Common.Lessons.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Schedule.Generation;

/// <summary>
/// Фоновая служба генерации расписания: читает задачи из <see cref="ScheduleGenerationQueue"/>
/// и выполняет каждую в собственном DI-scope через MediatR. Солвер (до 180 с) работает здесь,
/// а не в HTTP-потоке. Задачи обрабатываются последовательно; сбой одной не останавливает цикл.
/// </summary>
public sealed class ScheduleGenerationHostedService(
    ScheduleGenerationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduleGenerationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба фоновой генерации расписания запущена.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try { jobId = await queue.DequeueAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }

            await ProcessAsync(jobId, stoppingToken);
        }
    }

    private async Task ProcessAsync(Guid jobId, CancellationToken stoppingToken)
    {
        if (!queue.TryGetStatus(jobId, out var job))
            return;

        queue.MarkRunning(jobId);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new GenerateInstituteScheduleCommand { SemesterId = job.SemesterId, InstituteId = job.InstituteId },
                stoppingToken);

            queue.MarkSucceeded(jobId, result);
            logger.LogInformation("Генерация {JobId} завершена: {Status}, занятий создано {Count}.",
                jobId, result.Status, result.LessonsCreated);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            queue.MarkFailed(jobId, "Сервис остановлен во время генерации.");
        }
        catch (Exception ex)
        {
            queue.MarkFailed(jobId, ex.Message);
            logger.LogError(ex, "Генерация {JobId} завершилась ошибкой.", jobId);
        }
    }
}
