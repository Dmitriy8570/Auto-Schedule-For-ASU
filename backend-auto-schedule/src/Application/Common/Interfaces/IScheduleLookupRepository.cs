using Application.Common.DTO.Schedule;

namespace Application.Common.Interfaces;

/// <summary>
/// Справочники для редактора расписания: временные слоты недели (для сопоставления ячейки сетки
/// с TimeSlotId) и учебные планы (для выбора дисциплины/преподавателя/типа при добавлении пары).
/// </summary>
public interface IScheduleLookupRepository
{
    /// <summary>Временные слоты конкретной недели (день недели + номер пары).</summary>
    Task<IReadOnlyList<TimeSlotDto>> GetTimeSlotsAsync(Guid weekId, CancellationToken ct);

    /// <summary>
    /// Учебные планы, отфильтрованные по группе и/или преподавателю и/или семестру.
    /// Используется в форме «добавить пару».
    /// </summary>
    Task<IReadOnlyList<CurriculumOptionDto>> GetCurriculumsAsync(
        Guid? groupId, Guid? teacherId, Guid? semesterId, CancellationToken ct);
}
