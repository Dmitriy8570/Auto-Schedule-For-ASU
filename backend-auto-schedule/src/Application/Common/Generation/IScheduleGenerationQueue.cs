namespace Application.Common.Generation;

/// <summary>
/// Очередь фоновой понедельной генерации расписания: ставит задачу, отдаёт её статус и позволяет
/// отменить. Сама генерация выполняется фоновой службой, поэтому HTTP-поток не блокируется на
/// время работы солвера (минуты на семестр).
/// </summary>
public interface IScheduleGenerationQueue
{
    /// <summary>
    /// Поставить задачу генерации в очередь; возвращает её начальный статус (Queued).
    /// <paramref name="instituteId"/> == <c>null</c> — генерация по всему университету.
    /// </summary>
    GenerationJobStatus Enqueue(Guid semesterId, Guid? instituteId);

    /// <summary>Текущий статус задачи; <c>false</c>, если задача с таким идентификатором неизвестна.</summary>
    bool TryGetStatus(Guid jobId, out GenerationJobStatus status);

    /// <summary>
    /// Запросить отмену задачи. Уже сформированные недели остаются в БД. Возвращает <c>false</c>,
    /// если задача неизвестна или уже завершена.
    /// </summary>
    bool Cancel(Guid jobId);
}
