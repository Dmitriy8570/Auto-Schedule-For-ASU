using Application.Common.Lessons.Commands;
using Domain.workload;

namespace Application.Common.DTO.Generation;

/// <summary>Запись истории автогенерации: что и как сгенерировалось для института на семестр.</summary>
public sealed record GenerationRunDto(
    Guid Id,
    Guid SemesterId,
    string SemesterName,
    Guid InstituteId,
    string InstituteName,
    bool Succeeded,
    string Status,
    int LessonsCreated,
    double ObjectiveValue,
    double WallTimeSeconds,
    int UnplacedCount,
    IReadOnlyList<WorkloadShortfall> Unplaced,
    string? Error,
    DateTime CreatedAt,
    DateTime CompletedAt);

/// <summary>
/// Строка нераспределённой нагрузки: запланировано пар против фактически поставленных в расписании
/// (по текущему состоянию). Включаются только нагрузки с дефицитом (поставлено меньше плана).
/// </summary>
public sealed record UnplacedWorkloadRow(
    Guid CurriculumId,
    string Teacher,
    string Subject,
    string Groups,
    LessonType LessonType,
    int PlannedPairs,
    int PlacedPairs,
    int UnplacedPairs);
