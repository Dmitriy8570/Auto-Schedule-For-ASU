using Application.Common.DTO.Lookups;

namespace Application.Common.Interfaces;

/// <summary>Чтение корпусов и аудиторий для выбора занятости аудитории.</summary>
public interface IBuildingRepository
{
    Task<IReadOnlyList<BuildingDto>> GetBuildingsAsync(string? search, CancellationToken ct);

    Task<IReadOnlyList<RoomDto>> GetRoomsAsync(Guid? buildingId, string? search, CancellationToken ct);
}
