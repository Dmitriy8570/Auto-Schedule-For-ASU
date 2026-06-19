using Application.Common.DTO.Lookups;
using Application.Common.DTO.Management;
using Application.Common.Interfaces;
using Domain.constraints.equipments;
using Domain.university.buildings;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>EF Core-реализация управления аудиториями, оборудованием и весами ограничений.</summary>
public sealed class ManagementRepository(ApplicationDbContext context) : IManagementRepository
{
    // ----- Аудитории -----

    public async Task<IReadOnlyList<RoomDto>> GetClassroomsAsync(Guid? buildingId, CancellationToken ct)
    {
        var query = context.Classrooms.AsNoTracking();
        if (buildingId is { } id)
            query = query.Where(r => r.BuildingId == id);

        return await query
            .OrderBy(r => r.Building.Name).ThenBy(r => r.Name)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.BuildingId, r.Building.Name))
            .ToListAsync(ct);
    }

    public async Task<RoomDto> CreateClassroomAsync(string name, int capacity, Guid buildingId, CancellationToken ct)
    {
        var classroom = Classroom.Create(Guid.NewGuid(), name, capacity, buildingId);
        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync(ct);
        return await ProjectClassroomAsync(classroom.Id, ct)
            ?? new RoomDto(classroom.Id, classroom.Name, classroom.Capacity, classroom.BuildingId, string.Empty);
    }

    public async Task<RoomDto?> UpdateClassroomAsync(Guid id, string name, int capacity, Guid buildingId, CancellationToken ct)
    {
        var classroom = await context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (classroom is null) return null;

        classroom.Update(name, capacity, buildingId);
        await context.SaveChangesAsync(ct);
        return await ProjectClassroomAsync(id, ct);
    }

    public async Task<bool> DeleteClassroomAsync(Guid id, CancellationToken ct)
    {
        var classroom = await context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (classroom is null) return false;

        context.Classrooms.Remove(classroom);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private Task<RoomDto?> ProjectClassroomAsync(Guid id, CancellationToken ct) =>
        context.Classrooms
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.BuildingId, r.Building.Name))
            .FirstOrDefaultAsync(ct);

    // ----- Оборудование -----

    public async Task<IReadOnlyList<EquipmentDto>> GetEquipmentsAsync(CancellationToken ct) =>
        await context.Equipments
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentDto(e.Id, e.Name))
            .ToListAsync(ct);

    public async Task<EquipmentDto> CreateEquipmentAsync(string name, CancellationToken ct)
    {
        var equipment = Equipment.Create(Guid.NewGuid(), name);
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync(ct);
        return new EquipmentDto(equipment.Id, equipment.Name);
    }

    public async Task<EquipmentDto?> UpdateEquipmentAsync(Guid id, string name, CancellationToken ct)
    {
        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (equipment is null) return null;

        equipment.Rename(name);
        await context.SaveChangesAsync(ct);
        return new EquipmentDto(equipment.Id, equipment.Name);
    }

    public async Task<bool> DeleteEquipmentAsync(Guid id, CancellationToken ct)
    {
        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (equipment is null) return false;

        context.Equipments.Remove(equipment);
        await context.SaveChangesAsync(ct);
        return true;
    }

    // ----- Веса мягких ограничений -----

    public async Task<IReadOnlyList<ConstraintConfigDto>> GetConstraintsAsync(CancellationToken ct) =>
        await context.ConstraintConfigs
            .AsNoTracking()
            .OrderBy(c => c.ConstraintType)
            .Select(c => new ConstraintConfigDto(c.Id, c.ConstraintType, c.Penalty))
            .ToListAsync(ct);

    public async Task<ConstraintConfigDto?> UpdateConstraintPenaltyAsync(Guid id, int penalty, CancellationToken ct)
    {
        var config = await context.ConstraintConfigs.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (config is null) return null;

        config.SetPenalty(penalty);
        await context.SaveChangesAsync(ct);
        return new ConstraintConfigDto(config.Id, config.ConstraintType, config.Penalty);
    }
}
