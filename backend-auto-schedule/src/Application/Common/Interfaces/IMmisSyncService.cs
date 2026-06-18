namespace Application.Common.Interfaces;

/// <summary>
/// Синхронизация данных из внешней БД ММИС «Деканат» (MS SQL) в БД Auto-Schedule (PostgreSQL):
/// справочники и учебная нагрузка переносятся идемпотентно, изменения нагрузки фиксируются
/// в журналах (SemesterLog / WeekLog).
/// </summary>
public interface IMmisSyncService
{
    Task<MmisSyncResult> SyncAsync(CancellationToken cancellationToken);
}

/// <summary>Итог одного прогона синхронизации (для логирования и статуса).</summary>
public sealed record MmisSyncResult(
    int WorkloadsAdded,
    int WorkloadsUpdated,
    int WorkloadsDeleted,
    int ReferenceUpserts,
    TimeSpan Duration)
{
    /// <summary>Были ли какие-либо изменения нагрузки в этом прогоне.</summary>
    public bool HasWorkloadChanges => WorkloadsAdded > 0 || WorkloadsUpdated > 0 || WorkloadsDeleted > 0;
}
