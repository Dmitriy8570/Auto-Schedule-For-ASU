using Application.Common.Interfaces;

namespace Infrastructure.Mmis;

/// <summary>
/// Потокобезопасный холдер результата последней синхронизации (singleton). Полезен для логов
/// и диагностики: когда был последний прогон, что изменилось, не упал ли он.
/// </summary>
public sealed class MmisSyncStatus
{
    private readonly object _gate = new();

    public DateTimeOffset? LastRunAt { get; private set; }
    public bool LastRunSucceeded { get; private set; }
    public string? LastError { get; private set; }
    public MmisSyncResult? LastResult { get; private set; }

    public void RecordSuccess(MmisSyncResult result)
    {
        lock (_gate)
        {
            LastRunAt = DateTimeOffset.Now;
            LastRunSucceeded = true;
            LastError = null;
            LastResult = result;
        }
    }

    public void RecordFailure(Exception ex)
    {
        lock (_gate)
        {
            LastRunAt = DateTimeOffset.Now;
            LastRunSucceeded = false;
            LastError = ex.Message;
            LastResult = null;
        }
    }
}
