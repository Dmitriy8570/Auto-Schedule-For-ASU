using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Оснащение аудиторий оборудованием (EquipmentRoom) через UI-репозиторий: полная замена набора
/// идемпотентна и не плодит дублей при составном ключе (EquipmentId, ClassroomId).
/// </summary>
public sealed class ClassroomEquipmentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ClassroomEquipmentRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SetClassroomEquipment_ReplacesSetIdempotently()
    {
        // Готовим аудитории и каталог оборудования через сидер (без оснащения комнат).
        var seeder = new InfrastructureSeeder(
            _context, Options.Create(new InfrastructureSeedOptions { EquipRoomsFraction = 0 }),
            NullLogger<InfrastructureSeeder>.Instance);
        await seeder.SeedFacilitiesAsync(CancellationToken.None);
        await seeder.SeedEquipmentAsync(CancellationToken.None);
        _context.ChangeTracker.Clear();

        var classroomId = await _context.Classrooms.Select(c => c.Id).FirstAsync();
        var equipmentIds = await _context.Equipments.Select(e => e.Id).Take(3).ToListAsync();

        var repo = new ConstraintConfigurationRepository(_context);

        // Назначаем 3 единицы оборудования.
        var ok = await repo.SetClassroomEquipmentAsync(classroomId, equipmentIds, CancellationToken.None);
        Assert.True(ok);
        _context.ChangeTracker.Clear();
        var current = await repo.GetClassroomEquipmentAsync(classroomId, CancellationToken.None);
        Assert.Equal(3, current!.Count);

        // Сужаем набор до 1 — старые связи должны удалиться, дублей нет.
        await repo.SetClassroomEquipmentAsync(classroomId, [equipmentIds[0]], CancellationToken.None);
        _context.ChangeTracker.Clear();
        current = await repo.GetClassroomEquipmentAsync(classroomId, CancellationToken.None);
        Assert.Single(current!);
        Assert.Equal(equipmentIds[0], current![0]);
    }

    [Fact]
    public async Task GetClassroomEquipment_UnknownClassroom_ReturnsNull()
    {
        var repo = new ConstraintConfigurationRepository(_context);
        var result = await repo.GetClassroomEquipmentAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
