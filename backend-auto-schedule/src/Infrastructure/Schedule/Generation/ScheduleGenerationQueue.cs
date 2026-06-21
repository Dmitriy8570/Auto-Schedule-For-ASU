using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.Common.Generation;
using Application.Common.Lessons.Commands;

namespace Infrastructure.Schedule.Generation;

/// <summary>
/// In-memory очередь фоновой генерации: канал идентификаторов задач + словарь их статусов.
/// Регистрируется синглтоном. Продюсер — контроллер (<see cref="Enqueue"/>), единственный
/// консьюмер — <see cref="ScheduleGenerationHostedService"/> (задачи выполняются последовательно).
/// </summary>
public sealed class ScheduleGenerationQueue : IScheduleGenerationQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<Guid, GenerationJobStatus> _jobs = new();

    public GenerationJobStatus Enqueue(Guid semesterId, Guid instituteId)
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
        _channel.Writer.TryWrite(status.JobId); // запись в неограниченный канал всегда успешна
        return status;
    }

    public bool TryGetStatus(Guid jobId, out GenerationJobStatus status)
        => _jobs.TryGetValue(jobId, out status!);

    /// <summary>Дождаться следующей задачи из очереди (вызывает фоновая служба).</summary>
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);

    public void MarkRunning(Guid jobId) =>
        Update(jobId, s => s with { State = GenerationJobState.Running, StartedAt = DateTime.UtcNow });

    public void MarkSucceeded(Guid jobId, GenerateScheduleResult result) =>
        Update(jobId, s => s with
        {
            State = GenerationJobState.Succeeded,
            CompletedAt = DateTime.UtcNow,
            Result = result
        });

    public void MarkFailed(Guid jobId, string error) =>
        Update(jobId, s => s with
        {
            State = GenerationJobState.Failed,
            CompletedAt = DateTime.UtcNow,
            Error = error
        });

    private void Update(Guid jobId, Func<GenerationJobStatus, GenerationJobStatus> transform)
    {
        // Статус каждой задачи обновляет только единственный консьюмер, поэтому гонок по ключу нет.
        if (_jobs.TryGetValue(jobId, out var current))
            _jobs[jobId] = transform(current);
    }
}
