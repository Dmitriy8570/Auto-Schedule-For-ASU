using Domain.schedule;

namespace Application.Common.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Текущее расписание института (его занятия) — используется перед перегенерацией для удаления.</summary>
    Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken cancellationToken);

    /// <summary>Занятия института в конкретном семестре.</summary>
    Task<IReadOnlyList<Lesson>> GetByInstituteAndSemesterAsync(
        Guid instituteId, Guid semesterId, CancellationToken cancellationToken);

    /// <summary>Все занятия семестра.</summary>
    Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken cancellationToken);

    /// <summary>Удалить набор занятий (старое расписание).</summary>
    void RemoveRange(IEnumerable<Lesson> lessons);
    
    /// <summary>Занятия преподавателя; при заданном <paramref name="weekId"/> — только в пределах этой недели.</summary>
    Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken cancellationToken);

    /// <summary>Занятия в аудитории; при заданном <paramref name="weekId"/> — только в пределах этой недели.</summary>
    Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken cancellationToken);

    /// <summary>Занятия учебной группы; при заданном <paramref name="weekId"/> — только в пределах этой недели.</summary>
    Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken cancellationToken);
}
