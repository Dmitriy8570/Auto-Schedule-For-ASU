using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

/// <summary>
/// SignalR-хаб реального времени. После входа клиентское приложение (SPA сотрудника бюро)
/// устанавливает постоянное соединение и получает серверные события об изменении расписания
/// и учебной нагрузки. Канал односторонний (сервер → клиенты): рассылка выполняется через
/// <see cref="IHubContext{ScheduleHub}"/> из <see cref="SignalRRealtimeNotifier"/>, поэтому
/// серверных методов, вызываемых клиентом, хаб не содержит. Подключение требует аутентификации
/// наравне с REST API.
/// </summary>
[Authorize]
public sealed class ScheduleHub : Hub
{
}
