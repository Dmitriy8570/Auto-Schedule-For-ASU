using System.Text.Json;
using Application.Common.Generation;
using Application.Common.Interfaces;
using Application.Common.Lessons.Commands;
using Domain.schedule;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Schedule.Generation;

/// <summary>
/// Фоновая служба понедельной генерации расписания: читает задачи из <see cref="ScheduleGenerationQueue"/>
/// и выполняет каждую в собственном DI-scope через MediatR. Солвер работает здесь, а не в HTTP-потоке.
/// Прогресс по неделям пробрасывается в очередь, отмена связывается с токеном задачи. Каждый
/// завершённый запуск (успех, ошибка, отмена) фиксируется в истории генерации (БД).
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

        // Токен задачи (пользовательская отмена) связываем с токеном остановки службы.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken, queue.GetCancellationToken(jobId));

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            var mediator = sp.GetRequiredService<IMediator>();
            var progress = new DelegateProgress<GenerationProgress>(p => queue.SetProgress(jobId, p));

            var result = await mediator.Send(
                new GenerateScheduleCommand
                {
                    SemesterId = job.SemesterId,
                    InstituteId = job.InstituteId,
                    Progress = progress,
                },
                linked.Token);

            queue.MarkSucceeded(jobId, result);
            logger.LogInformation("Генерация {JobId} завершена: {Status}, занятий создано {Count}.",
                jobId, result.Status, result.LessonsCreated);

            if (result.Unplaced.Count > 0)
                logger.LogWarning(
                    "Генерация {JobId}: {Count} нагрузок размещены не полностью.",
                    jobId, result.Unplaced.Count);

            await PersistRunAsync(sp, job, succeeded: true, result.Status, result, error: null, stoppingToken);
        }
        catch (OperationCanceledException) when (queue.IsCancellationRequested(jobId) && !stoppingToken.IsCancellationRequested)
        {
            // Пользовательская отмена: уже сформированные недели сохранены (понедельная запись).
            queue.MarkCancelled(jobId, partial: null);
            logger.LogInformation("Генерация {JobId} отменена пользователем.", jobId);
            await PersistRunAsync(sp, job, succeeded: false, "Cancelled", result: null, error: null, CancellationToken.None);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Сервис останавливается: токен отменён, запись истории не пройдёт — не пишем.
            queue.MarkFailed(jobId, "Сервис остановлен во время генерации.");
        }
        catch (Exception ex)
        {
            queue.MarkFailed(jobId, ex.Message);
            logger.LogError(ex, "Генерация {JobId} завершилась ошибкой.", jobId);
            await PersistRunAsync(sp, job, succeeded: false, "Failed", result: null, ex.Message, CancellationToken.None);
        }
    }

    /// <summary>Зафиксировать завершённый запуск в истории (ошибка записи не валит цикл).</summary>
    private async Task PersistRunAsync(
        IServiceProvider sp, GenerationJobStatus job,
        bool succeeded, string status, GenerateScheduleResult? result, string? error, CancellationToken ct)
    {
        try
        {
            var (semesterName, instituteName) = await ResolveNamesAsync(sp, job, ct);
            var run = GenerationRun.Create(
                Guid.NewGuid(),
                job.SemesterId, semesterName, job.InstituteId ?? Guid.Empty, instituteName,
                succeeded, status,
                result?.LessonsCreated ?? 0, result?.ObjectiveValue ?? 0, result?.WallTimeSeconds ?? 0,
                result?.Unplaced.Count ?? 0,
                JsonSerializer.Serialize(result?.Unplaced ?? Array.Empty<WorkloadShortfall>()),
                error,
                job.CreatedAt, DateTime.UtcNow);
            await sp.GetRequiredService<IGenerationRunRepository>().AddAsync(run, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить историю генерации {JobId}.", job.JobId);
        }
    }

    /// <summary>Денормализованные названия семестра (диапазон дат) и института (или «Весь университет»).</summary>
    private static async Task<(string Semester, string Institute)> ResolveNamesAsync(
        IServiceProvider sp, GenerationJobStatus job, CancellationToken ct)
    {
        var ctx = sp.GetRequiredService<ApplicationDbContext>();

        var sem = await ctx.Semesters.AsNoTracking()
            .Where(s => s.Id == job.SemesterId)
            .Select(s => new { s.StartDate, s.EndDate })
            .FirstOrDefaultAsync(ct);

        var semesterName = sem is null
            ? job.SemesterId.ToString()
            : $"{sem.StartDate:dd.MM.yyyy} — {sem.EndDate:dd.MM.yyyy}";

        string instituteName;
        if (job.InstituteId is { } iid)
            instituteName = await ctx.Institutes.AsNoTracking()
                .Where(i => i.Id == iid)
                .Select(i => i.Name)
                .FirstOrDefaultAsync(ct) ?? iid.ToString();
        else
            instituteName = "Весь университет";

        return (semesterName, instituteName);
    }

    /// <summary>Синхронный приёмник прогресса: обновляет статус задачи без захвата контекста синхронизации.</summary>
    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
