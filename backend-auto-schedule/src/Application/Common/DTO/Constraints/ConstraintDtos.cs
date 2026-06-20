using Domain.constraints;

namespace Application.Common.DTO.Constraints;

/// <summary>
/// Ячейка сетки доступности: градация желательности слота (день недели + номер пары)
/// для преподавателя или аудитории. <c>State = Neutral</c> означает отсутствие ограничения.
/// </summary>
public sealed record AvailabilityCellDto(int DayOfWeek, int PairNumber, AvailabilityState State);

/// <summary>
/// По-нагрузочные ограничения учебного плана (правая панель «Нагрузка» вкладки «Ограничения»):
/// требуемое оборудование, параллельность подгрупп, двойная пара и предпочтительный корпус.
/// </summary>
public sealed record CurriculumConstraintsDto(
    IReadOnlyList<Guid> RequiredEquipmentIds,
    bool IsParallel,
    bool IsDouble,
    Guid? PreferredBuildingId);
