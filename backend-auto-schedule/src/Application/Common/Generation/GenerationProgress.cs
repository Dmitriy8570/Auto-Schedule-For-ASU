using Domain.calendar;

namespace Application.Common.Generation;

/// <summary>
/// Прогресс понедельной генерации расписания: какая неделя сейчас обрабатывается. Передаётся
/// командой генерации в очередь через <see cref="System.IProgress{T}"/>, чтобы фронтенд показывал
/// «неделя N из M» без полноэкранной блокировки.
/// </summary>
public sealed record GenerationProgress(
    int CurrentWeek,
    int TotalWeeks,
    string WeekLabel,
    WeekType WeekType);
