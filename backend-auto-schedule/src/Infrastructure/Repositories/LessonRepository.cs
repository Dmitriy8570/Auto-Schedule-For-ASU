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

    public async Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, CancellationToken cancellationToken) =>
        await _context.Lessons.Where(l => l.Stream.Curriculums.Any(c => c.TeacherId == teacherId)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, CancellationToken cancellationToken) =>
        await _context.Lessons.Where(l => l.Stream.StreamGroups.Any(sg => sg.GroupId == groupId)).ToListAsync(cancellationToken);
    
    public async Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, CancellationToken cancellationToken) =>
        await _context.Lessons.Where(l => l.ClassroomId == classroomId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken cancellationToken) =>
        await _context.Lessons
            .Where(l => l.Stream.StreamGroups
                .Any(sg => sg.Group.Course.Degree.InstituteId == instituteId))
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<Lesson> lessons) => _context.Lessons.RemoveRange(lessons);
}