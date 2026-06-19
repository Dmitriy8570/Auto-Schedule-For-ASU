using Domain.calendar;
using Domain.schedule;
using Domain.workload;

namespace Application.Common.DTO.Lessons;

/// <summary>
/// Занятие расписания с полями для отображения в сетке «дни × пары».
/// «Сырые» идентификаторы сохранены для обратной совместимости; обогащённые поля
/// (день, пара, дисциплина, ФИО, аудитория, тип, чётность недели) заполняются при наличии
/// загруженных навигаций. Если у занятия не задан учебный план, поля дисциплины/преподавателя/типа
/// будут <c>null</c>.
/// </summary>
public class LessonDTO
{
    public Guid Id { get; init; }
    public Guid ClassroomId { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid StreamId { get; init; }
    public Guid? CurriculumId { get; init; }
    public ScheduleVersion Version { get; init; }

    // ----- Обогащённые поля для сетки -----

    /// <summary>День недели: 0 = понедельник … 5 = суббота (соответствует <see cref="WeekDayType"/>).</summary>
    public int DayOfWeek { get; init; }

    /// <summary>Номер пары в течение дня (1, 2, …).</summary>
    public int PairNumber { get; init; }

    /// <summary>Учебная неделя занятия.</summary>
    public Guid WeekId { get; init; }

    /// <summary>Тип недели (чередование): Red / Blue.</summary>
    public WeekType WeekType { get; init; }

    public string? SubjectName { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }

    /// <summary>Тип занятия (Lecture/Seminar/Laboratory/Consultation/Examination); null — план не задан.</summary>
    public LessonType? LessonType { get; init; }

    public string ClassroomName { get; init; } = string.Empty;
    public string BuildingName { get; init; } = string.Empty;

    /// <summary>Группы потока через запятую.</summary>
    public string GroupNames { get; init; } = string.Empty;

    public int StudentsCount { get; init; }

    /// <summary>
    /// Спроецировать доменное занятие в DTO. Ожидает загруженные навигации
    /// (TimeSlot→WeekDay→Week, Classroom→Building, Curriculum→Subject/Teacher, Stream→StreamGroups→Group).
    /// </summary>
    public static LessonDTO From(Lesson l) => new()
    {
        Id = l.Id,
        ClassroomId = l.ClassroomId,
        TimeSlotId = l.TimeSlotId,
        StreamId = l.StreamId,
        CurriculumId = l.CurriculumId,
        Version = l.Version,

        DayOfWeek = l.TimeSlot is { WeekDay: { } wd } ? (int)wd.DayOfWeek : 0,
        PairNumber = l.TimeSlot?.Number ?? 0,
        WeekId = l.TimeSlot?.WeekDay?.WeekId ?? Guid.Empty,
        WeekType = l.TimeSlot?.WeekDay?.Week?.WeekType ?? WeekType.Red,

        SubjectName = l.Curriculum?.Subject?.Name,
        TeacherId = l.Curriculum?.TeacherId,
        TeacherName = l.Curriculum?.Teacher?.Name,
        LessonType = l.Curriculum?.LessonType,

        ClassroomName = l.Classroom?.Name ?? string.Empty,
        BuildingName = l.Classroom?.Building?.Name ?? string.Empty,

        GroupNames = l.Stream?.StreamGroups is { Count: > 0 } sg
            ? string.Join(", ", sg.Where(x => x.Group != null).Select(x => x.Group.Name))
            : string.Empty,
        StudentsCount = l.Stream?.StudentsCount ?? 0,
    };
}
