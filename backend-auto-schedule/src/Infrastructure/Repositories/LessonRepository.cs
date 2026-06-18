using Application.Common.Interfaces;
using Domain.schedule;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class  LessonRepository: ILessonRepository
{
    private readonly ApplicationDbContext _context;

    public LessonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken)
          => await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken)
          => await _context.Lessons.AddAsync(lesson, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
          => await _context.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.Stream.Curriculums.Any(c => c.TeacherId == teacherId))
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.Stream.StreamGroups.Any(sg => sg.GroupId == groupId))
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.ClassroomId == classroomId)
            .Where(InWeek(weekId))
            .ToListAsync(cancellationToken);

    /// <summary>Фильтр по неделе через TimeSlot → WeekDay → Week; если неделя не задана — пропускает всё.</summary>
    private static System.Linq.Expressions.Expression<Func<Lesson, bool>> InWeek(Guid? weekId) =>
        weekId is null
            ? _ => true
            : l => l.TimeSlot.WeekDay.WeekId == weekId.Value;

    public async Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.Stream.StreamGroups
                .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetByInstituteAndSemesterAsync(
        Guid instituteId, Guid semesterId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.SemesterId == semesterId
                && l.Stream.StreamGroups.Any(sg => sg.Group.Course.Degree.InstituteId == instituteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.SemesterId == semesterId)
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<Lesson> lessons) => _context.Lessons.RemoveRange(lessons);
}