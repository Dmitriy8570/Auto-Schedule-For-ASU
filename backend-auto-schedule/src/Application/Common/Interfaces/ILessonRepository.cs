using Domain.schedule;

namespace Application.Common.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Текущее расписание института (его занятия) — используется перед перегенерацией для удаления.</summary>
    Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken cancellationToken);

    /// <summary>Удалить набор занятий (старое расписание института).</summary>
    void RemoveRange(IEnumerable<Lesson> lessons);
    
    Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, CancellationToken cancellationToken);
}
