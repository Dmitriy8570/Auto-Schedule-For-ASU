using Domain.calendar;
using Infrastructure.Data;
using Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тесты сидера инфраструктуры на SQLite (in-memory): аудиторный фонд и календарная сетка
/// (рабочие дни + пары) наполняются корректно и идемпотентно — повторный прогон не плодит дубли.
/// Это оси «аудитории»/«время» солвера, без которых генерация Infeasible.
/// </summary>
public sealed class InfrastructureSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public InfrastructureSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private InfrastructureSeeder CreateSeeder(InfrastructureSeedOptions? opts = null) =>
        new(_context, Options.Create(opts ?? new InfrastructureSeedOptions()),
            NullLogger<InfrastructureSeeder>.Instance);

    [Fact]
    public async Task SeedFacilities_PopulatesDefaultBuildingsAndClassrooms()
    {
        var seeder = CreateSeeder();

        var rooms = await seeder.SeedFacilitiesAsync(CancellationToken.None);

        Assert.True(rooms > 0);
        Assert.Equal(3, await _context.Buildings.CountAsync());
        Assert.Equal(rooms, await _context.Classrooms.CountAsync());
        Assert.All(await _context.Classrooms.ToListAsync(), c => Assert.True(c.Capacity > 0));
    }

    [Fact]
    public async Task SeedFacilities_IsIdempotent()
    {
        var seeder = CreateSeeder();

        await seeder.SeedFacilitiesAsync(CancellationToken.None);
        var firstCount = await _context.Classrooms.CountAsync();

        var second = await seeder.SeedFacilitiesAsync(CancellationToken.None);

        Assert.Equal(0, second); // ничего не добавлено
        Assert.Equal(firstCount, await _context.Classrooms.CountAsync());
    }

    [Fact]
    public async Task SeedCalendarGrid_CreatesDaysAndPairsForEachWeek()
    {
        var (_, weekId) = await SeedSemesterWithWeekAsync();
        var seeder = CreateSeeder(new InfrastructureSeedOptions { WorkingDays = 6, PairsPerDay = 8 });

        var weeks = await seeder.SeedCalendarGridAsync(CancellationToken.None);

        Assert.Equal(1, weeks);
        Assert.Equal(6, await _context.WeekDays.CountAsync(d => d.WeekId == weekId));
        Assert.Equal(6 * 8, await _context.TimeSlots.CountAsync());
        // Дни идут с понедельника (0) по субботу (5).
        var days = await _context.WeekDays.Select(d => d.DayOfWeek).ToListAsync();
        Assert.Contains(WeekDayType.Monday, days);
        Assert.Contains(WeekDayType.Saturday, days);
        Assert.DoesNotContain(WeekDayType.Sunday, days);
    }

    [Fact]
    public async Task SeedCalendarGrid_IsIdempotent()
    {
        await SeedSemesterWithWeekAsync();
        var seeder = CreateSeeder();

        await seeder.SeedCalendarGridAsync(CancellationToken.None);
        var slots = await _context.TimeSlots.CountAsync();

        var second = await seeder.SeedCalendarGridAsync(CancellationToken.None);

        Assert.Equal(0, second); // неделя уже имеет сетку — пропущена
        Assert.Equal(slots, await _context.TimeSlots.CountAsync());
    }

    [Fact]
    public async Task SeedCalendarGrid_RespectsConfiguredDimensions()
    {
        await SeedSemesterWithWeekAsync();
        var seeder = CreateSeeder(new InfrastructureSeedOptions { WorkingDays = 5, PairsPerDay = 6 });

        await seeder.SeedCalendarGridAsync(CancellationToken.None);

        Assert.Equal(5, await _context.WeekDays.CountAsync());
        Assert.Equal(5 * 6, await _context.TimeSlots.CountAsync());
    }

    private async Task<(Guid SemesterId, Guid WeekId)> SeedSemesterWithWeekAsync()
    {
        var semester = Semester.Create(Guid.NewGuid(),
            new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30));
        var week = Week.Create(Guid.NewGuid(),
            new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 7), WeekType.Red, semester.Id);

        _context.Semesters.Add(semester);
        _context.Weeks.Add(week);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return (semester.Id, week.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
