using Application.Common.Exceptions;
using Domain.calendar;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.common;
using Domain.university.groups;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тесты серверной проверки коллизий при ручном добавлении занятия (LessonRepository.FindConflictsAsync)
/// на SQLite: пересечение по аудитории и по группе в одном слоте обнаруживается, а свободный
/// слот/аудитория конфликтов не даёт.
/// </summary>
public sealed class LessonConflictTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    private readonly Guid _semesterId = Guid.NewGuid();
    private readonly Guid _slot1 = Guid.NewGuid();
    private readonly Guid _slot2 = Guid.NewGuid();
    private readonly Guid _roomA = Guid.NewGuid();
    private readonly Guid _roomB = Guid.NewGuid();
    private readonly Guid _groupShared = Guid.NewGuid();
    private readonly Guid _stream1 = Guid.NewGuid();
    private readonly Guid _stream2 = Guid.NewGuid();

    public LessonConflictTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        Seed();
    }

    [Fact]
    public async Task FindConflicts_SameRoomSameSlot_ReportsClassroomConflict()
    {
        var repo = new LessonRepository(_context);

        // Стрим 2 в той же аудитории и слоте, что и существующее занятие стрима 1.
        var conflicts = await repo.FindConflictsAsync(_roomA, _slot1, _stream2, null, CancellationToken.None);

        Assert.Contains(conflicts, c => c.Kind == ScheduleConflictKind.Classroom);
    }

    [Fact]
    public async Task FindConflicts_FreeSlot_NoConflict()
    {
        var repo = new LessonRepository(_context);

        // Другой слот и другая аудитория — конфликтов нет (стрим 2 групп с стримом 1 не делит... делит!).
        // Берём стрим без общих групп: используем _stream2, но в свободном слоте — групповой конфликт
        // проверяется по слоту, поэтому в пустом слоте 2 конфликтов быть не должно.
        var conflicts = await repo.FindConflictsAsync(_roomB, _slot2, _stream2, null, CancellationToken.None);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task FindConflicts_SharedGroupSameSlot_ReportsGroupConflict()
    {
        var repo = new LessonRepository(_context);

        // Другая аудитория (нет конфликта аудитории), но стрим 2 делит группу со стримом 1 в том же слоте.
        var conflicts = await repo.FindConflictsAsync(_roomB, _slot1, _stream2, null, CancellationToken.None);

        Assert.Contains(conflicts, c => c.Kind == ScheduleConflictKind.Group);
        Assert.DoesNotContain(conflicts, c => c.Kind == ScheduleConflictKind.Classroom);
    }

    private void Seed()
    {
        var institute = Institute.Create(Guid.NewGuid(), "ИМИТ");
        var degree = Degree.Create(Guid.NewGuid(), TypeDegree.Bachelor, institute.Id);
        var course = Course.Create(Guid.NewGuid(), 1, degree.Id);
        var group = Group.Create(_groupShared, "БИ-101", Shift.First, 25, course.Id);

        var building = Building.Create(Guid.NewGuid(), "Корпус А");
        var roomA = Classroom.Create(_roomA, "А-101", 30, building.Id);
        var roomB = Classroom.Create(_roomB, "А-102", 30, building.Id);

        var semester = Semester.Create(_semesterId, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30));
        var week = Week.Create(Guid.NewGuid(), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 7), WeekType.Red, semester.Id);
        var weekDay = WeekDay.Create(Guid.NewGuid(), week.Id, WeekDayType.Monday);
        var slot1 = TimeSlot.Create(_slot1, weekDay.Id, 1);
        var slot2 = TimeSlot.Create(_slot2, weekDay.Id, 2);

        // Оба потока содержат одну и ту же группу — основа для группового конфликта.
        var stream1 = AcademicStream.Create(_stream1, 25);
        var stream2 = AcademicStream.Create(_stream2, 25);
        var link1 = StreamGroups.Create(group.Id, stream1.Id);
        var link2 = StreamGroups.Create(group.Id, stream2.Id);

        // Существующее занятие: стрим 1 в аудитории A, слот 1.
        var lesson = Lesson.Create(Guid.NewGuid(), _roomA, _slot1, _stream1, _semesterId);

        _context.AddRange(institute, degree, course, group, building, roomA, roomB,
            semester, week, weekDay, slot1, slot2, stream1, stream2, link1, link2, lesson);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
