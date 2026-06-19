using Application.Common.DTO.Calendar;

namespace Application.Common.Interfaces;

/// <summary>Чтение календаря (семестры и недели) для выбора периода расписания.</summary>
public interface ICalendarRepository
{
    /// <summary>Семестры, отсортированы по дате начала по убыванию (свежие сверху).</summary>
    Task<IReadOnlyList<SemesterDto>> GetSemestersAsync(CancellationToken ct);

    /// <summary>Недели семестра с порядковым номером, отсортированы по дате начала.</summary>
    Task<IReadOnlyList<WeekDto>> GetWeeksAsync(Guid semesterId, CancellationToken ct);
}
