using Application.Common.DTO.Lookups;
using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>Чтение корпусов и аудиторий (read-only, серверная проекция в DTO).</summary>
public sealed class BuildingRepository(ApplicationDbContext context) : IBuildingRepository
{
    /// <summary>Потолок размера выборки аудиторий, чтобы не грузить БД на каждый вызов.</summary>
    private const int MaxResults = 20;

    public async Task<IReadOnlyList<BuildingDto>> GetBuildingsAsync(string? search, CancellationToken ct)
    {
        var query = context.Buildings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{search}%"));

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BuildingDto(b.Id, b.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(Guid? buildingId, string? search, CancellationToken ct)
    {
        var query = context.Classrooms.AsNoTracking();

        if (buildingId is { } id)
            query = query.Where(r => r.BuildingId == id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => EF.Functions.ILike(r.Name, $"%{search}%"));

        return await query
            .OrderBy(r => r.Name)
            .Take(MaxResults)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.BuildingId, r.Building.Name))
            .ToListAsync(ct);
    }
}
