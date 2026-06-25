using Application.Common.Interfaces;
using Domain.calendar;
using Domain.constraints;
using Domain.constraints.equipments;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
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

        // Оснащаем аудитории «по роли» (детерминированно по вместимости/индексу), чтобы требования
        // оборудования на планах (см. SeedCurriculumRequirementsAsync) имели где выполняться:
        //   • поточные залы (≥90): проектор + звуковая система;
        //   • средние (≥45): проектор;
        //   • часть малых (каждая 3-я) — компьютерные классы: ПК + лабораторное оборудование;
        //   • все: маркерная доска.
        var byName = names.ToDictionary(n => n, n => DeterministicGuid.For($"Equipment:{n}", 0));
        Guid? Eq(string n) => byName.TryGetValue(n, out var id) ? id : null;

        var rooms = await db.Classrooms.OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Capacity }).ToListAsync(ct);
        var seenLinks = new HashSet<(Guid Equip, Guid Room)>();
        int links = 0;
        void Equip(Guid roomId, string equipmentName)
        {
            if (Eq(equipmentName) is { } equipmentId && seenLinks.Add((equipmentId, roomId)))
            {
                db.EquipmentRooms.Add(EquipmentRoom.Create(equipmentId, roomId));
                links++;
            }
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            Equip(room.Id, EquipmentCatalog.Board);
            if (room.Capacity >= 90) { Equip(room.Id, EquipmentCatalog.Projector); Equip(room.Id, EquipmentCatalog.Sound); }
            else if (room.Capacity >= 45) Equip(room.Id, EquipmentCatalog.Projector);

            if (room.Capacity < 90 && i % 3 == 0)
            {
                Equip(room.Id, EquipmentCatalog.Computers);
                Equip(room.Id, EquipmentCatalog.LabEquipment);
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

        // Значения по умолчанию. Вес окна подобран эмпирически (см. прогон солвера на когорте): при
        // 5 оставалось ~8 окон, при 50 окна исчезают полностью при почти той же компактности. Вес
        // окна должен быть ощутимо выше прочих мягких штрафов (доступность 10, любимый корпус 5,
        // дневной перебор 50), чтобы солвер делал учебный день сплошным, но НИЖЕ веса учебного дня
        // (GroupDayUsage = 200 в SolverPenaltyWeights), иначе он начнёт размазывать пары по дням ради
        // устранения окна. Все веса далее настраиваются из UI (вкладка «Ограничения» → «Веса»).
        var defaults = new (ConstraintType Type, int Penalty)[]
        {
            (ConstraintType.TeacherGap, 40),
            (ConstraintType.StudentGap, 50),
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

    public async Task<int> SeedCurriculumRequirementsAsync(CancellationToken ct)
    {
        if (!_options.SeedCurriculumRequirements) return 0;

        // Требования статичны относительно планов: если хоть одно уже есть — пропускаем.
        if (await db.NeededEquipments.AnyAsync(ct)) return 0;

        var equipmentByName = await db.Equipments.ToDictionaryAsync(e => e.Name, e => e.Id, ct);
        Guid? Eq(string name) => equipmentByName.TryGetValue(name, out var id) ? id : null;

        var curricula = await db.Curriculums.Select(c => new { c.Id, c.LessonType }).ToListAsync(ct);
        int links = 0;
        foreach (var c in curricula)
        {
            // Лаба — ПК + лабораторное оборудование; лекция — проектор; семинар — без требований.
            var required = c.LessonType switch
            {
                LessonType.Laboratory => new[] { Eq(EquipmentCatalog.Computers), Eq(EquipmentCatalog.LabEquipment) },
                LessonType.Lecture => new[] { Eq(EquipmentCatalog.Projector) },
                _ => Array.Empty<Guid?>(),
            };

            foreach (var equipmentId in required)
                if (equipmentId is { } id)
                {
                    db.NeededEquipments.Add(NeededEquipment.Create(c.Id, id));
                    links++;
                }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг требований оборудования на планы: {Links} требований на {Curricula} планов.",
            links, curricula.Count);
        return links;
    }

    public async Task<int> SeedTeacherAvailabilityAsync(CancellationToken ct)
    {
        if (!_options.SeedTeacherAvailability) return 0;

        // Доступность статична: если уже есть записи — пропускаем.
        if (await db.TeacherAvailabilities.AnyAsync(ct)) return 0;

        var fraction = Math.Clamp(_options.EveningTeachersFraction, 0, 1);
        if (fraction <= 0) return 0;

        var teacherIds = await db.Teachers.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync(ct);
        if (teacherIds.Count == 0) return 0;

        int days = Math.Clamp(_options.WorkingDays, 1, 7);
        int step = (int)Math.Max(1, Math.Round(1 / fraction));

        // «Вечерним» преподавателям ранние пары (1-2) нежелательны — мягкий штраф (Discouraged).
        // Группы в dev-данных первой смены (жёстко пары 1-4), поэтому жёсткий «только вечер» сделал бы
        // расписание неосуществимым; мягкая модель сдвигает их занятия к поздним из доступных пар.
        int evening = 0;
        for (int i = 0; i < teacherIds.Count; i += step)
        {
            for (int d = 0; d < days; d++)
                for (int number = 1; number <= 2; number++)
                    db.TeacherAvailabilities.Add(TeacherAvailability.Create(
                        teacherIds[i], (WeekDayType)d, number, AvailabilityStates.DiscouragedPenalty));
            evening++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг доступности преподавателей: {Count} «вечерних».", evening);
        return evening;
    }

    public async Task<int> SeedFavoriteBuildingsAsync(CancellationToken ct)
    {
        if (!_options.SeedFavoriteBuildings) return 0;

        var buildingIds = await db.Buildings.OrderBy(b => b.Name).Select(b => b.Id).ToListAsync(ct);
        var instituteIds = await db.Institutes.OrderBy(i => i.Name).Select(i => i.Id).ToListAsync(ct);
        if (buildingIds.Count == 0 || instituteIds.Count == 0) return 0;

        // Институт → «домашний» корпус (детерминированно, по порядку имён).
        var buildingByInstitute = new Dictionary<Guid, Guid>();
        for (int i = 0; i < instituteIds.Count; i++)
            buildingByInstitute[instituteIds[i]] = buildingIds[i % buildingIds.Count];

        // Обновляем только планы без заданного корпуса — идемпотентно и переживает ресинк ММИС
        // (синхронизация FavoriteBuildingId не трогает).
        var curricula = await db.Curriculums
            .Include(c => c.Teacher).ThenInclude(t => t.Department)
            .Where(c => c.FavoriteBuildingId == null)
            .ToListAsync(ct);

        int updated = 0;
        foreach (var c in curricula)
            if (buildingByInstitute.TryGetValue(c.Teacher.Department.InstituteId, out var buildingId))
            {
                c.SetFavoriteBuilding(buildingId);
                updated++;
            }

        if (updated > 0) await db.SaveChangesAsync(ct);
        logger.LogInformation("Сидинг предпочтительных корпусов: обновлено планов — {Count}.", updated);
        return updated;
    }
}
