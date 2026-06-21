namespace Application.Common.Generation;

/// <summary>
/// Очередь фоновой генерации расписания института: ставит задачу в очередь и отдаёт её статус.
/// Сама генерация выполняется фоновой службой, поэтому HTTP-поток не блокируется на время
/// работы солвера (до 180 с).
/// </summary>
public interface IScheduleGenerationQueue
{
    /// <summary>Поставить задачу генерации в очередь; возвращает её начальный статус (Queued).</summary>
    GenerationJobStatus Enqueue(Guid semesterId, Guid instituteId);

    /// <summary>Текущий статус задачи; <c>false</c>, если задача с таким идентификатором неизвестна.</summary>
    bool TryGetStatus(Guid jobId, out GenerationJobStatus status);
}
