using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

/// <summary>
/// Реализация <see cref="IRealtimeNotifier"/> поверх SignalR: рассылает события всем подключённым
/// клиентам через <see cref="IHubContext{ScheduleHub}"/>. Имена событий («ScheduleChanged»,
/// «WorkloadChanged») совпадают с подписками клиентского приложения.
/// </summary>
public sealed class SignalRRealtimeNotifier(IHubContext<ScheduleHub> hub) : IRealtimeNotifier
{
    public Task NotifyScheduleChangedAsync(Guid instituteId, string changeType, CancellationToken cancellationToken)
        => hub.Clients.All.SendAsync(
            "ScheduleChanged", new { instituteId, changeType }, cancellationToken);

    public Task NotifyWorkloadChangedAsync(int added, int updated, int deleted, CancellationToken cancellationToken)
        => hub.Clients.All.SendAsync(
            "WorkloadChanged", new { added, updated, deleted }, cancellationToken);
}
