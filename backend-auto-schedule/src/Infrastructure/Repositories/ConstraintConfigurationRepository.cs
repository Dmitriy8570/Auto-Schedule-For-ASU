using Application.Common.DTO.Constraints;
using Application.Common.Interfaces;
using Domain.calendar;
using Domain.constraints;
using Domain.university.groups;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// EF Core-реализация конфигурации ограничений солвера: сетки доступности преподавателей и
/// аудиторий, по-нагрузочные правила учебных планов и смена групп.
/// </summary>
public sealed class ConstraintConfigurationRepository(ApplicationDbContext context) : IConstraintConfigurationRepository
{
    // ----- Доступность преподавателя -----

    public async Task<IReadOnlyList<AvailabilityCellDto>?> GetTeacherAvailabilityAsync(Guid teacherId, CancellationToken ct)
    {
        if (!await context.Teachers.AnyAsync(t => t.Id == teacherId, ct))
            return null;

        // FromPenalty не транслируется в SQL — материализуем слоты, затем маппим в памяти.
        var slots = await context.TeacherAvailabilities
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => new { a.DayOfWeek, a.NumberLesson, a.Penalty })
            .ToListAsync(ct);

        return slots
            .Select(a => new AvailabilityCellDto(
                (int)a.DayOfWeek, a.NumberLesson, AvailabilityStates.FromPenalty(a.Penalty)))
            .ToList();
    }

    public async Task<bool> SetTeacherAvailabilityAsync(Guid teacherId, IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct)
    {
        var teacher = await context.Teachers
            .Include(t => t.TeacherAvailabilities)
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null) return false;

        teacher.ReplaceAvailabilities(ToCells(cells));
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- Доступность аудитории -----

    public async Task<IReadOnlyList<AvailabilityCellDto>?> GetClassroomAvailabilityAsync(Guid classroomId, CancellationToken ct)
    {
        if (!await context.Classrooms.AnyAsync(c => c.Id == classroomId, ct))
            return null;

        // FromPenalty не транслируется в SQL — материализуем слоты, затем маппим в памяти.
        var slots = await context.ClassroomAvailabilities
            .AsNoTracking()
            .Where(a => a.ClassroomId == classroomId)
            .Select(a => new { a.DayOfWeek, a.NumberLesson, a.Penalty })
            .ToListAsync(ct);

        return slots
            .Select(a => new AvailabilityCellDto(
                (int)a.DayOfWeek, a.NumberLesson, AvailabilityStates.FromPenalty(a.Penalty)))
            .ToList();
    }

    public async Task<bool> SetClassroomAvailabilityAsync(Guid classroomId, IReadOnlyList<AvailabilityCellDto> cells, CancellationToken ct)
    {
        var classroom = await context.Classrooms
            .Include(c => c.ClassroomAvailabilities)
            .FirstOrDefaultAsync(c => c.Id == classroomId, ct);
        if (classroom is null) return false;

        classroom.ReplaceAvailabilities(ToCells(cells));
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- Оснащение аудитории оборудованием -----

    public async Task<IReadOnlyList<Guid>?> GetClassroomEquipmentAsync(Guid classroomId, CancellationToken ct)
    {
        if (!await context.Classrooms.AnyAsync(c => c.Id == classroomId, ct))
            return null;

        return await context.EquipmentRooms
            .AsNoTracking()
            .Where(er => er.ClassroomId == classroomId)
            .Select(er => er.EquipmentId)
            .ToListAsync(ct);
    }

    public async Task<bool> SetClassroomEquipmentAsync(Guid classroomId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct)
    {
        var classroom = await context.Classrooms
            .Include(c => c.EquipmentRooms)
            .FirstOrDefaultAsync(c => c.Id == classroomId, ct);
        if (classroom is null) return false;

        await EnsureEquipmentExistAsync(equipmentIds, ct);
        classroom.SetEquipment(equipmentIds);
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- По-нагрузочные ограничения учебного плана -----

    public async Task<CurriculumConstraintsDto?> GetCurriculumConstraintsAsync(Guid curriculumId, CancellationToken ct) =>
        await context.Curriculums
            .AsNoTracking()
            .Where(c => c.Id == curriculumId)
            .Select(c => new CurriculumConstraintsDto(
                c.NeededEquipments.Select(n => n.EquipmentId).ToList(),
                c.Parallelism,
                c.Double,
                c.FavoriteBuildingId))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> SetCurriculumConstraintsAsync(Guid curriculumId, CurriculumConstraintsDto constraints, CancellationToken ct)
    {
        var curriculum = await context.Curriculums
            .Include(c => c.NeededEquipments)
            .FirstOrDefaultAsync(c => c.Id == curriculumId, ct);
        if (curriculum is null) return false;

        await EnsureEquipmentExistAsync(constraints.RequiredEquipmentIds, ct);
        await EnsureBuildingExistsAsync(constraints.PreferredBuildingId, ct);

        curriculum.SetConstraints(constraints.IsParallel, constraints.IsDouble, constraints.PreferredBuildingId);
        curriculum.SetRequiredEquipment(constraints.RequiredEquipmentIds);
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- Смена группы -----

    public async Task<bool> SetGroupShiftAsync(Guid groupId, Shift shift, CancellationToken ct)
    {
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return false;

        group.SetShift(shift);
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- Вспомогательное -----

    private static IEnumerable<(WeekDayType, int, AvailabilityState)> ToCells(IEnumerable<AvailabilityCellDto> cells) =>
        cells.Select(c => ((WeekDayType)c.DayOfWeek, c.PairNumber, c.State));

    private async Task EnsureEquipmentExistAsync(IReadOnlyList<Guid> equipmentIds, CancellationToken ct)
    {
        var ids = equipmentIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var found = await context.Equipments.CountAsync(e => ids.Contains(e.Id), ct);
        if (found != ids.Count)
            throw new ValidationException([new ValidationFailure(
                nameof(CurriculumConstraintsDto.RequiredEquipmentIds), "Указано несуществующее оборудование.")]);
    }

    private async Task EnsureBuildingExistsAsync(Guid? buildingId, CancellationToken ct)
    {
        if (buildingId is not { } id) return;
        if (!await context.Buildings.AnyAsync(b => b.Id == id, ct))
            throw new ValidationException([new ValidationFailure(
                nameof(CurriculumConstraintsDto.PreferredBuildingId), "Указан несуществующий корпус.")]);
    }
}
