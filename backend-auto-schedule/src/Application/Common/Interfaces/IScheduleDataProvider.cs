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

    /// <summary>
    /// Поставляет данные для генерации расписания одного института (декомпозиция «B + C»).
    /// В отличие от <see cref="GetAsync"/>, нагрузки ограничены институтом, а в
    /// <see cref="ScheduleData"/> дополнительно заполняются занятые ресурсы других институтов
    /// этого семестра и якорь к расписанию прошлого семестра.
    /// </summary>
    Task<ScheduleData> GetForInstituteAsync(
        Guid semesterId, Guid instituteId, CancellationToken cancellationToken);
}
