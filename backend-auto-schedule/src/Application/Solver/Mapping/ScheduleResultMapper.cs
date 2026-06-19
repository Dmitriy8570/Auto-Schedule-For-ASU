using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.schedule;

namespace Application.Solver.Mapping;

/// <summary>
/// Сопоставляет индексы осей модели (нагрузка, аудитория, слот) с идентификаторами доменных
/// сущностей и создаёт черновики занятий <see cref="Lesson"/>.
/// Требует загруженной навигации SemesterWorkload → Curriculum.
/// </summary>
public sealed class ScheduleResultMapper : IScheduleResultMapper
{
    public IReadOnlyList<Lesson> ToLessons(ScheduleData data, IReadOnlyList<Assignment> assignments)
    {
        var lessons = new List<Lesson>(assignments.Count);
        foreach (var a in assignments)
        {
            var curriculum = data.SemesterWorkloads[a.Workload].Curriculum;
            var streamId = curriculum.StreamId;
            var classroomId = data.Classrooms[a.Room].Id;
            var timeSlotId = data.TimeSlots[a.Slot].Id;

            lessons.Add(Lesson.Create(
                Guid.NewGuid(), classroomId, timeSlotId, streamId, data.SemesterId, curriculum.Id));
        }
        return lessons;
    }
}
