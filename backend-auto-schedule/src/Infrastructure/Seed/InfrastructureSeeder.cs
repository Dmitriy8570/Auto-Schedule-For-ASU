using Application.Common.Interfaces;
using Domain.calendar;
using Domain.constraints.equipments;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Infrastructure.Data;
using Infrastructure.Mmis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Seed;

/// <summary>
/// EF Core-реализация сидера инфраструктуры. Корпуса/аудитории и календарная сетка
/// (рабочие дни + пары) не приходят из ММИС, поэтому без сидинга у солвера нет осей
/// «аудитории» и «время». Идентификаторы детерминированы (<see cref="DeterministicGuid"/>),
/// а каждая фаза гейтится проверкой наличия, поэтому повторные прогоны безопасны.
/// </summary>
public sealed class InfrastructureSeeder(
    ApplicationDbContext db,
    IOptions<InfrastructureSeedOptions> options,
    ILogger<InfrastructureSeeder> logger) : IInfrastructureSeeder
{
    private readonly InfrastructureSeedOptions _options = options.Value;

    public async Task<int> SeedFacilitiesAsync(CancellationToken ct)
    {
        if (!_options.SeedFacilities) return 0;

        // Аудиторный фонд статичен: если хотя бы один корпус уже есть — считаем фонд наполненным.
        if (await db.Buildings.AnyAsync(ct)) return 0;

        var buildings = _options.Buildings is { Length: > 0 }
            ? _options.Buildings
            : InfrastructureSeedOptions.DefaultBuildings();

        int rooms = 0;
        foreach (var b in buildings)
        {
            var buildingId = DeterministicGuid.For($"Building:{b.Name}", 0);
            db.Buildings.Add(Building.Create(buildingId, b.Name));

            foreach (var c in b.Classrooms)
            {
                var classroomId = DeterministicGuid.For($"Classroom:{b.Name}:{c.Name}", 0);
                db.Classrooms.Add(Classroom.Create(classroomId, c.Name, c.Capacity, buildingId));
                rooms++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг аудиторного фонда: {Buildings} корпусов, {Rooms} аудиторий.",
            buildings.Length, rooms);
        return rooms;
    }

    public async Task<int> SeedEquipmentAsync(CancellationToken ct)
    {
        if (!_options.SeedEquipment) return 0;

        // Каталог оборудования статичен: если хотя бы один тип уже есть — считаем каталог наполненным.
        if (await db.Equipments.AnyAsync(ct)) return 0;

        var names = _options.Equipment is { Length: > 0 }
            ? _options.Equipment
            : InfrastructureSeedOptions.DefaultEquipment();

        var equipmentIds = new List<Guid>();
        foreach (var name in names)
        {
            var id = DeterministicGuid.For($"Equipment:{name}", 0);
            db.Equipments.Add(Equipment.Create(id, name));
            equipmentIds.Add(id);
        }

        // Оснащаем часть аудиторий «универсальным» оборудованием (первые два типа каталога —
        // проектор и ПК), чтобы ограничение оборудования в солвере имело данные. Детерминированно
        // по индексу аудитории, поэтому повторный прогон дал бы тот же набор (но он гейтится выше).
        int links = 0;
        var fraction = Math.Clamp(_options.EquipRoomsFraction, 0, 1);
        if (fraction > 0 && equipmentIds.Count > 0)
        {
            var roomIds = await db.Classrooms.OrderBy(c => c.Name).Select(c => c.Id).ToListAsync(ct);
            var universal = equipmentIds.Take(2).ToList();
            int step = (int)Math.Max(1, Math.Round(1 / fraction));
            for (int i = 0; i < roomIds.Count; i += step)
                foreach (var equipmentId in universal)
                {
                    db.EquipmentRooms.Add(EquipmentRoom.Create(equipmentId, roomIds[i]));
                    links++;
                }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг оборудования: {Types} типов, оснащено связей «аудитория↔оборудование» — {Links}.",
            names.Length, links);
        return names.Length;
    }

    public async Task<int> SeedConstraintWeightsAsync(CancellationToken ct)
    {
        if (!_options.SeedConstraintWeights) return 0;

        // Веса мягких ограничений — по одной записи на тип. Если хоть одна уже есть — пропускаем.
        if (await db.ConstraintConfigs.AnyAsync(ct)) return 0;

        // Значения по умолчанию: окна штрафуются умеренно, нарушение доступности — сильнее.
        // Все веса далее настраиваются из UI (вкладка «Ограничения» → «Веса»).
        var defaults = new (ConstraintType Type, int Penalty)[]
        {
            (ConstraintType.TeacherGap, 3),
            (ConstraintType.StudentGap, 5),
            (ConstraintType.ClassroomAvailability, 10),
            (ConstraintType.TeacherAvailability, 10),
        };

        foreach (var (type, penalty) in defaults)
        {
            var id = DeterministicGuid.For($"ConstraintConfig:{type}", 0);
            db.ConstraintConfigs.Add(ConstraintConfig.Create(id, type, penalty));
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг весов мягких ограничений: {Count} типов.", defaults.Length);
        return defaults.Length;
    }

    public async Task<int> SeedCalendarGridAsync(CancellationToken ct)
    {
        if (!_options.SeedCalendarGrid) return 0;

        int days = Math.Clamp(_options.WorkingDays, 1, 7);
        int pairs = Math.Clamp(_options.PairsPerDay, 1, 12);

        // Недели, у которых ещё нет ни одного рабочего дня (новые после ММИС-синка).
        var weekIds = await db.Weeks
            .Where(w => !w.WeekDays.Any())
            .Select(w => w.Id)
            .ToListAsync(ct);
        if (weekIds.Count == 0) return 0;

        foreach (var weekId in weekIds)
        {
            for (int d = 0; d < days; d++)
            {
                var dayType = (WeekDayType)d; // 0 = Monday … 5 = Saturday
                var weekDayId = DeterministicGuid.For($"WeekDay:{weekId}", d);
                db.WeekDays.Add(WeekDay.Create(weekDayId, weekId, dayType));

                for (int number = 1; number <= pairs; number++)
                {
                    var slotId = DeterministicGuid.For($"TimeSlot:{weekDayId}", number);
                    db.TimeSlots.Add(TimeSlot.Create(slotId, weekDayId, number));
                }
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Сидинг календарной сетки: {Weeks} недель × {Days} дней × {Pairs} пар = {Slots} слотов.",
            weekIds.Count, days, pairs, weekIds.Count * days * pairs);
        return weekIds.Count;
    }
}
