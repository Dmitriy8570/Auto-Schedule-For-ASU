using Domain.calendar;

namespace Application.Common.DTO.Calendar;

/// <summary>Учебный семестр для выбора в UI.</summary>
public sealed record SemesterDto(Guid Id, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, int WeekCount);

/// <summary>
/// Учебная неделя семестра. <see cref="Number"/> — порядковый номер недели в семестре (1..N),
/// вычисляется по возрастанию даты начала; UI оперирует именно номером.
/// </summary>
public sealed record WeekDto(Guid Id, int Number, WeekType WeekType, DateOnly StartDate, DateOnly EndDate);
