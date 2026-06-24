using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.Common.Generation;
using Application.Common.Lessons.Commands;

namespace Infrastructure.Schedule.Generation;

/// <summary>
/// In-memory очередь фоновой генерации: канал идентификаторов задач + словарь их статусов +
/// источники отмены по задачам. Регистрируется синглтоном. Продюсер — контроллер (<see cref="Enqueue"/>),
/// единственный консьюмер — <see cref="ScheduleGenerationHostedService"/> (задачи выполняются
/// последовательно). Прогресс и отмену задаёт служба/контроллер через методы ниже.
/// </summary>
public sealed class ScheduleGenerationQueue : IScheduleGenerationQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<Guid, GenerationJobStatus> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();

    public GenerationJobStatus Enqueue(Guid semesterId, Guid? instituteId)
    {
        var status = new GenerationJobStatus(
            JobId: Guid.NewGuid(),
            State: GenerationJobState.Queued,
            SemesterId: semesterId,
            InstituteId: instituteId,
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            Result: null,
            Error: null);

        _jobs[status.JobId] = status;
        _cancellations[status.JobId] = new CancellationTokenSource();
        _channel.Writer.TryWrite(status.JobId); // запись в неограниченный канал всегда успешна
        return status;
    }

    public bool TryGetStatus(Guid jobId, out GenerationJobStatus status)
        => _jobs.TryGetValue(jobId, out status!);

    public bool Cancel(Guid jobId)
    {
        if (!_cancellations.TryGetValue(jobId, out var cts)) return false;
        if (!_jobs.TryGetValue(jobId, out var job)
            || job.State is GenerationJobState.Succeeded or GenerationJobState.Failed or GenerationJobState.Cancelled)
            return false;

        cts.Cancel();
        return true;
    }

    /// <summary>Дождаться следующей задачи из очереди (вызывает фоновая служба).</summary>
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>Источник отмены задачи (фоновая служба связывает его токен со своим).</summary>
    public CancellationToken GetCancellationToken(Guid jobId)
        => _cancellations.TryGetValue(jobId, out var cts) ? cts.Token : CancellationToken.None;

    /// <summary>Запрошена ли отмена задачи пользователем.</summary>
    public bool IsCancellationRequested(Guid jobId)
        => _cancellations.TryGetValue(jobId, out var cts) && cts.IsCancellationRequested;

    public void MarkRunning(Guid jobId) =>
        Update(jobId, s => s with { State = GenerationJobState.Running, StartedAt = DateTime.UtcNow });

    public void SetProgress(Guid jobId, GenerationProgress progress) =>
        Update(jobId, s => s with { Progress = progress });

    public void MarkSucceeded(Guid jobId, GenerateScheduleResult result)
    {
        Update(jobId, s => s with
        {
            State = GenerationJobState.Succeeded,
            CompletedAt = DateTime.UtcNow,
            Result = result
        });
        DisposeCancellation(jobId);
    }

    public void MarkFailed(Guid jobId, string error)
    {
        Update(jobId, s => s with
        {
            State = GenerationJobState.Failed,
            CompletedAt = DateTime.UtcNow,
            Error = error
        });
        DisposeCancellation(jobId);
    }

    public void MarkCancelled(Guid jobId, GenerateScheduleResult? partial)
    {
        Update(jobId, s => s with
        {
            State = GenerationJobState.Cancelled,
            CompletedAt = DateTime.UtcNow,
            Result = partial ?? s.Result
        });
        DisposeCancellation(jobId);
    }

    private void DisposeCancellation(Guid jobId)
    {
        if (_cancellations.TryRemove(jobId, out var cts)) cts.Dispose();
    }

    private void Update(Guid jobId, Func<GenerationJobStatus, GenerationJobStatus> transform)
    {
        // Статус каждой задачи обновляет только единственный консьюмер, поэтому гонок по ключу нет.
        if (_jobs.TryGetValue(jobId, out var current))
            _jobs[jobId] = transform(current);
    }
}
