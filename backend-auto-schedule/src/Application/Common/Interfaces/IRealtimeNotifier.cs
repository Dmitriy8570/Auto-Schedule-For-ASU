namespace Application.Common.Interfaces;

/// <summary>
/// Рассылает подключённым клиентам уведомления об изменениях, чтобы интерфейсы сотрудников бюро
/// расписаний обновлялись в режиме реального времени, без перезагрузки страницы и ручной сверки.
/// Абстракция объявлена в слое Application; конкретная реализация (поверх SignalR) находится в
/// слое представления и подключается в композиционном корне приложения.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// Расписание института изменилось (опубликовано, перегенерировано или сброшено) — клиентам
    /// следует обновить отображаемую сетку расписания.
    /// </summary>
    Task NotifyScheduleChangedAsync(Guid instituteId, string changeType, CancellationToken cancellationToken);

    /// <summary>
    /// Учебная нагрузка изменилась по итогам синхронизации с системой «Нагрузка ВУЗа»
    /// (добавлено/изменено/удалено строк) — клиентам следует обновить представление нагрузки.
    /// </summary>
    Task NotifyWorkloadChangedAsync(int added, int updated, int deleted, CancellationToken cancellationToken);
}
