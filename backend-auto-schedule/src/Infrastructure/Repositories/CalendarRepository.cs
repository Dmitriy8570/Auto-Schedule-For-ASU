using Application.Common.DTO.Calendar;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>Чтение календаря (семестры/недели). Все запросы read-only.</summary>
public sealed class CalendarRepository(ApplicationDbContext context) : ICalendarRepository
{
    public async Task<IReadOnlyList<SemesterDto>> GetSemestersAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SemesterDto(
                s.Id,
                s.StartDate,
                s.EndDate,
                today >= s.StartDate && today <= s.EndDate,
                s.Weeks.Count))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WeekDto>> GetWeeksAsync(Guid semesterId, CancellationToken ct)
    {
        var weeks = await context.Weeks
            .AsNoTracking()
            .Where(w => w.SemesterId == semesterId)
            .OrderBy(w => w.StartDate)
            .Select(w => new { w.Id, w.WeekType, w.StartDate, w.EndDate })
            .ToListAsync(ct);

        // Порядковый номер недели (1..N) присваивается по возрастанию даты начала.
        return weeks
            .Select((w, i) => new WeekDto(w.Id, i + 1, w.WeekType, w.StartDate, w.EndDate))
            .ToList();
    }
}
