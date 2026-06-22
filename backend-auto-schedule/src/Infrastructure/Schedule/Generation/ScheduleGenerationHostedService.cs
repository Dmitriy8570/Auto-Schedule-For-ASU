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
/// Фоновая служба генерации расписания: читает задачи из <see cref="ScheduleGenerationQueue"/>
/// и выполняет каждую в собственном DI-scope через MediatR. Солвер работает здесь, а не в
/// HTTP-потоке. Задачи обрабатываются последовательно; сбой одной не останавливает цикл.
/// Каждый завершённый запуск (успех или ошибка) фиксируется в истории генерации (БД).
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

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            var mediator = sp.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new GenerateInstituteScheduleCommand { SemesterId = job.SemesterId, InstituteId = job.InstituteId },
                stoppingToken);

            queue.MarkSucceeded(jobId, result);
            logger.LogInformation("Генерация {JobId} завершена: {Status}, занятий создано {Count}.",
                jobId, result.Status, result.LessonsCreated);

            if (result.Unplaced.Count > 0)
                logger.LogWarning(
                    "Генерация {JobId}: {Count} нагрузок размещены не полностью. {Details}",
                    jobId, result.Unplaced.Count,
                    string.Join("; ", result.Unplaced.Select(u =>
                        $"{u.Teacher} / {u.Subject} ({u.LessonType}): {u.PlacedPairs}/{u.PlannedPairs} пар — {u.Reason}")));

            await PersistRunAsync(sp, job, result, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Сервис останавливается: токен отменён, запись в БД не пройдёт — историю не пишем.
            queue.MarkFailed(jobId, "Сервис остановлен во время генерации.");
        }
        catch (Exception ex)
        {
            queue.MarkFailed(jobId, ex.Message);
            logger.LogError(ex, "Генерация {JobId} завершилась ошибкой.", jobId);
            await PersistFailureAsync(sp, job, ex.Message);
        }
    }

    /// <summary>Зафиксировать успешный запуск в истории (ошибка записи не валит цикл).</summary>
    private async Task PersistRunAsync(
        IServiceProvider sp, GenerationJobStatus job, GenerateScheduleResult result, CancellationToken ct)
    {
        try
        {
            var (semesterName, instituteName) = await ResolveNamesAsync(sp, job, ct);
            var run = GenerationRun.Create(
                Guid.NewGuid(),
                job.SemesterId, semesterName, job.InstituteId, instituteName,
                succeeded: true, status: result.Status,
                result.LessonsCreated, result.ObjectiveValue, result.WallTimeSeconds,
                result.Unplaced.Count, JsonSerializer.Serialize(result.Unplaced), error: null,
                job.CreatedAt, DateTime.UtcNow);
            await sp.GetRequiredService<IGenerationRunRepository>().AddAsync(run, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить историю генерации {JobId}.", job.JobId);
        }
    }

    /// <summary>Зафиксировать неудавшийся запуск в истории (вне токена остановки — запись завершается).</summary>
    private async Task PersistFailureAsync(IServiceProvider sp, GenerationJobStatus job, string error)
    {
        try
        {
            var (semesterName, instituteName) = await ResolveNamesAsync(sp, job, CancellationToken.None);
            var run = GenerationRun.Create(
                Guid.NewGuid(),
                job.SemesterId, semesterName, job.InstituteId, instituteName,
                succeeded: false, status: "Failed",
                0, 0, 0, 0, "[]", error,
                job.CreatedAt, DateTime.UtcNow);
            await sp.GetRequiredService<IGenerationRunRepository>().AddAsync(run, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить историю неудачной генерации {JobId}.", job.JobId);
        }
    }

    /// <summary>Денормализованные названия семестра (диапазон дат) и института на момент запуска.</summary>
    private static async Task<(string Semester, string Institute)> ResolveNamesAsync(
        IServiceProvider sp, GenerationJobStatus job, CancellationToken ct)
    {
        var ctx = sp.GetRequiredService<ApplicationDbContext>();

        var sem = await ctx.Semesters.AsNoTracking()
            .Where(s => s.Id == job.SemesterId)
            .Select(s => new { s.StartDate, s.EndDate })
            .FirstOrDefaultAsync(ct);

        var instituteName = await ctx.Institutes.AsNoTracking()
            .Where(i => i.Id == job.InstituteId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct);

        var semesterName = sem is null
            ? job.SemesterId.ToString()
            : $"{sem.StartDate:dd.MM.yyyy} — {sem.EndDate:dd.MM.yyyy}";

        return (semesterName, instituteName ?? job.InstituteId.ToString());
    }
}
