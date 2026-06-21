using Application.Common.Generation;
using Application.Common.Lessons.Commands;
using Infrastructure.Schedule.Generation;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Модульные тесты очереди фоновой генерации: постановка задачи, чтение статуса,
/// извлечение из канала и переходы состояний (Queued → Running → Succeeded/Failed).
/// </summary>
public sealed class ScheduleGenerationQueueTests
{
    [Fact]
    public void Enqueue_StoresQueuedJob_RetrievableByStatus()
    {
        var queue = new ScheduleGenerationQueue();
        var semester = Guid.NewGuid();
        var institute = Guid.NewGuid();

        var enq = queue.Enqueue(semester, institute);

        Assert.Equal(GenerationJobState.Queued, enq.State);
        Assert.Equal(semester, enq.SemesterId);
        Assert.Equal(institute, enq.InstituteId);

        Assert.True(queue.TryGetStatus(enq.JobId, out var got));
        Assert.Equal(enq.JobId, got.JobId);
        Assert.Equal(GenerationJobState.Queued, got.State);
    }

    [Fact]
    public void TryGetStatus_UnknownId_ReturnsFalse()
    {
        var queue = new ScheduleGenerationQueue();
        Assert.False(queue.TryGetStatus(Guid.NewGuid(), out _));
    }

    [Fact]
    public async Task DequeueAsync_ReturnsEnqueuedJobId()
    {
        var queue = new ScheduleGenerationQueue();
        var enq = queue.Enqueue(Guid.NewGuid(), Guid.NewGuid());

        var jobId = await queue.DequeueAsync(CancellationToken.None);

        Assert.Equal(enq.JobId, jobId);
    }

    [Fact]
    public void MarkRunning_ThenSucceeded_UpdatesStatusAndResult()
    {
        var queue = new ScheduleGenerationQueue();
        var enq = queue.Enqueue(Guid.NewGuid(), Guid.NewGuid());

        queue.MarkRunning(enq.JobId);
        Assert.True(queue.TryGetStatus(enq.JobId, out var running));
        Assert.Equal(GenerationJobState.Running, running.State);
        Assert.NotNull(running.StartedAt);

        var result = new GenerateScheduleResult("Optimal", 42, 7.0, 1.5);
        queue.MarkSucceeded(enq.JobId, result);

        Assert.True(queue.TryGetStatus(enq.JobId, out var done));
        Assert.Equal(GenerationJobState.Succeeded, done.State);
        Assert.NotNull(done.CompletedAt);
        Assert.Equal(42, done.Result!.LessonsCreated);
        Assert.Null(done.Error);
    }

    [Fact]
    public void MarkFailed_SetsErrorAndState()
    {
        var queue = new ScheduleGenerationQueue();
        var enq = queue.Enqueue(Guid.NewGuid(), Guid.NewGuid());

        queue.MarkFailed(enq.JobId, "boom");

        Assert.True(queue.TryGetStatus(enq.JobId, out var failed));
        Assert.Equal(GenerationJobState.Failed, failed.State);
        Assert.Equal("boom", failed.Error);
        Assert.NotNull(failed.CompletedAt);
    }
}
