using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.schedule;

namespace Application.Solver.Mapping;

/// <summary>Преобразует индексное решение солвера в доменные занятия.</summary>
public interface IScheduleResultMapper
{
    IReadOnlyList<Lesson> ToLessons(ScheduleData data, IReadOnlyList<Assignment> assignments);
}
