using Application.Common.Interfaces;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тестовая подделка <see cref="IRealtimeNotifier"/>: ничего не рассылает (без SignalR), но
/// запоминает факт вызова — этого достаточно для быстрых модульных тестов хендлеров.
/// </summary>
internal sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public int ScheduleChangedCalls { get; private set; }
    public int WorkloadChangedCalls { get; private set; }

    public Task NotifyScheduleChangedAsync(Guid instituteId, string changeType, CancellationToken cancellationToken)
    {
        ScheduleChangedCalls++;
        return Task.CompletedTask;
    }

    public Task NotifyWorkloadChangedAsync(int added, int updated, int deleted, CancellationToken cancellationToken)
    {
        WorkloadChangedCalls++;
        return Task.CompletedTask;
    }
}
