using Application.Solver.Model;
using Domain.calendar;

namespace Application.Common.Interfaces;

/// <summary>Краткое описание учебной недели семестра для понедельной генерации.</summary>
public sealed record ScheduleWeek(Guid Id, WeekType Type, DateOnly StartDate, DateOnly EndDate);

/// <summary>
/// Поставляет входные данные задачи составления расписания <em>по неделям</em>.
/// Реализация (в Infrastructure) обязана загрузить навигацию
/// <see cref="WorkloadItem.Curriculum"/> и оснащение/доступность аудиторий,
/// которые использует конвейер строителей модели.
/// </summary>
public interface IScheduleDataProvider
{
    /// <summary>Недели семестра по возрастанию даты начала — внешняя ось понедельной генерации.</summary>
    Task<IReadOnlyList<ScheduleWeek>> GetWeeksAsync(Guid semesterId, CancellationToken cancellationToken);

    /// <summary>
    /// Данные для генерации расписания одной недели.
    /// <list type="bullet">
    /// <item><paramref name="instituteId"/> == <c>null</c> — вся неделя (нагрузки всех институтов
    /// решаются совместно, без начальных блокировок);</item>
    /// <item>иначе — только нагрузки института; в данные дополнительно кладутся ресурсы, уже занятые
    /// в этой неделе расписанием других институтов (жёсткие блокировки).</item>
    /// </list>
    /// <see cref="ScheduleData.Anchors"/> заполняется мягким якорем к расписанию прошлого семестра
    /// (если оно есть); якорь к уже решённой неделе того же типа добавляет оркестратор.
    /// </summary>
    Task<ScheduleData> GetForWeekAsync(
        Guid semesterId, Guid weekId, Guid? instituteId, CancellationToken cancellationToken);
}
