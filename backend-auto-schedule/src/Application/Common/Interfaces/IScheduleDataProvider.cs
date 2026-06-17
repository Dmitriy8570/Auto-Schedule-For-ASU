using Application.Solver.Model;

namespace Application.Common.Interfaces;

/// <summary>
/// Поставляет входные данные задачи составления расписания на семестр.
/// Реализация (в Infrastructure) обязана загрузить навигацию
/// SemesterWorkload → Curriculum и оснащение/доступность аудиторий,
/// которые использует конвейер строителей модели.
/// </summary>
public interface IScheduleDataProvider
{
    Task<ScheduleData> GetAsync(Guid semesterId, CancellationToken cancellationToken);
}
