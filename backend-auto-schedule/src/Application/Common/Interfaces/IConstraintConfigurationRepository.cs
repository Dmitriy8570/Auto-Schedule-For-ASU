using Application.Common.DTO.Constraints;
using Domain.university.groups;

namespace Application.Common.Interfaces;

/// <summary>
/// Конфигурация ограничений солвера через UI (вкладка «Ограничения»): сетки доступности
/// преподавателей и аудиторий, по-нагрузочные правила учебных планов и смена групп.
/// Методы самодостаточны (сохраняют изменения сами). Возврат <c>null</c>/<c>false</c> —
/// целевой объект не найден.
/// </summary>
public interface IConstraintConfigurationRepository
{
    // ----- Доступность преподавателя -----
    Task<IReadOnlyList<AvailabilityCellDto>?> GetTeacherAvailabilityAsync(Guid teacherId, CancellationToken ct);
    Task<bool> SetTeacherAvailabilityAsync(Guid teacherId, IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct);

    // ----- Доступность аудитории -----
    Task<IReadOnlyList<AvailabilityCellDto>?> GetClassroomAvailabilityAsync(Guid classroomId, CancellationToken ct);
    Task<bool> SetClassroomAvailabilityAsync(Guid classroomId, IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct);

    // ----- Оснащение аудитории оборудованием -----
    Task<IReadOnlyList<Guid>?> GetClassroomEquipmentAsync(Guid classroomId, CancellationToken ct);
    Task<bool> SetClassroomEquipmentAsync(Guid classroomId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct);

    // ----- По-нагрузочные ограничения учебного плана -----
    Task<CurriculumConstraintsDto?> GetCurriculumConstraintsAsync(Guid curriculumId, CancellationToken ct);
    Task<bool> SetCurriculumConstraintsAsync(Guid curriculumId, CurriculumConstraintsDto constraints, CancellationToken ct);

    // ----- Смена группы -----
    Task<bool> SetGroupShiftAsync(Guid groupId, Shift shift, CancellationToken ct);
}
