using Domain.constraints;
using Domain.schedule;
using Domain.university.common;
using Domain.university.teachers;
using Domain.workload;
using Infrastructure.Data;
using Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тесты сидера ограничений на SQLite (in-memory): требования оборудования на планах, «вечерние»
/// преподаватели (доступность) и предпочтительный корпус наполняются корректно и идемпотентно.
/// </summary>
public sealed class ConstraintSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ConstraintSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    private InfrastructureSeeder Seeder(InfrastructureSeedOptions? opts = null) =>
        new(_context, Options.Create(opts ?? new InfrastructureSeedOptions()),
            NullLogger<InfrastructureSeeder>.Instance);

    [Fact]
    public async Task SeedCurriculumRequirements_AddsEquipmentByLessonType()
    {
        var seeder = Seeder();
        await seeder.SeedFacilitiesAsync(CancellationToken.None);
        await seeder.SeedEquipmentAsync(CancellationToken.None); // каталог оборудования
        var (_, _, lecture, lab) = await SeedAcademicGraphAsync();
        _context.ChangeTracker.Clear();

        var links = await seeder.SeedCurriculumRequirementsAsync(CancellationToken.None);

        Assert.Equal(3, links); // лекция: проектор (1) + лаба: ПК + лаб. оборудование (2)
        var projector = await EquipmentIdAsync(EquipmentCatalog.Projector);
        var computers = await EquipmentIdAsync(EquipmentCatalog.Computers);
        var lab1 = await EquipmentIdAsync(EquipmentCatalog.LabEquipment);

        var lectureEq = await _context.NeededEquipments.Where(n => n.CurriculumId == lecture).Select(n => n.EquipmentId).ToListAsync();
        var labEq = await _context.NeededEquipments.Where(n => n.CurriculumId == lab).Select(n => n.EquipmentId).ToListAsync();
        Assert.Equal(new[] { projector }, lectureEq);
        Assert.Equal(new[] { computers, lab1 }.OrderBy(x => x), labEq.OrderBy(x => x));

        // Идемпотентно.
        Assert.Equal(0, await seeder.SeedCurriculumRequirementsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SeedTeacherAvailability_MarksEveningTeachersOnEarlyPairs()
    {
        await SeedAcademicGraphAsync(extraTeachers: 4); // всего 5 преподавателей
        _context.ChangeTracker.Clear();
        var seeder = Seeder(new InfrastructureSeedOptions { WorkingDays = 6, EveningTeachersFraction = 0.2 });

        var evening = await seeder.SeedTeacherAvailabilityAsync(CancellationToken.None);

        Assert.Equal(1, evening); // шаг = round(1/0.2) = 5 → из 5 преподавателей выбран один
        var records = await _context.TeacherAvailabilities.ToListAsync();
        Assert.Equal(6 * 2, records.Count); // 6 дней × ранние пары 1-2
        Assert.All(records, r => Assert.True(r.NumberLesson is 1 or 2));
        Assert.All(records, r => Assert.Equal(AvailabilityStates.DiscouragedPenalty, r.Penalty));

        Assert.Equal(0, await seeder.SeedTeacherAvailabilityAsync(CancellationToken.None)); // идемпотентно
    }

    [Fact]
    public async Task SeedFavoriteBuildings_AssignsHomeBuildingByInstitute()
    {
        var seeder = Seeder();
        await seeder.SeedFacilitiesAsync(CancellationToken.None); // корпуса
        var (instituteId, _, lecture, lab) = await SeedAcademicGraphAsync();
        _context.ChangeTracker.Clear();

        var updated = await seeder.SeedFavoriteBuildingsAsync(CancellationToken.None);

        Assert.Equal(2, updated); // оба плана преподавателя института
        var expected = await _context.Buildings.OrderBy(b => b.Name).Select(b => b.Id).FirstAsync();
        var favorites = await _context.Curriculums
            .Where(c => c.Id == lecture || c.Id == lab)
            .Select(c => c.FavoriteBuildingId).ToListAsync();
        Assert.All(favorites, f => Assert.Equal(expected, f));

        Assert.Equal(0, await seeder.SeedFavoriteBuildingsAsync(CancellationToken.None)); // идемпотентно
    }

    private async Task<Guid> EquipmentIdAsync(string name) =>
        await _context.Equipments.Where(e => e.Name == name).Select(e => e.Id).FirstAsync();

    /// <summary>Минимальный корректный граф: институт → кафедра → преподаватель + дисциплина/поток + лекция и лаба.</summary>
    private async Task<(Guid InstituteId, Guid TeacherId, Guid Lecture, Guid Lab)> SeedAcademicGraphAsync(int extraTeachers = 0)
    {
        var institute = Institute.Create(Guid.NewGuid(), "Институт 1");
        var department = Department.Create(Guid.NewGuid(), "Кафедра 1", institute.Id);
        var teacher = Teacher.Create(Guid.NewGuid(), "Иванов И. И.", department.Id);
        var subject = Subject.Create(Guid.NewGuid(), "Дисциплина 1");
        var stream = AcademicStream.Create(Guid.NewGuid(), 30);

        var lecture = Curriculum.Create(Guid.NewGuid(), teacher.Id, stream.Id, subject.Id, LessonType.Lecture, true, false);
        var lab = Curriculum.Create(Guid.NewGuid(), teacher.Id, stream.Id, subject.Id, LessonType.Laboratory, false, false);

        _context.Institutes.Add(institute);
        _context.Departments.Add(department);
        _context.Teachers.Add(teacher);
        _context.Subjects.Add(subject);
        _context.AcademicStreams.Add(stream);
        _context.Curriculums.AddRange(lecture, lab);

        for (int i = 0; i < extraTeachers; i++)
            _context.Teachers.Add(Teacher.Create(Guid.NewGuid(), $"Преп {i}", department.Id));

        await _context.SaveChangesAsync();
        return (institute.Id, teacher.Id, lecture.Id, lab.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
