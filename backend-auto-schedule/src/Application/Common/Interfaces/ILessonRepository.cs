using Domain.schedule;

namespace Application.Common.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, CancellationToken cancellationToken);
}
