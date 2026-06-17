using Application.Common.DTO.Lookups;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lookups.Queries;

// ----- Корпуса -----
public sealed record GetBuildingsQuery(string? Search) : IRequest<IReadOnlyList<BuildingDto>>;

public sealed class GetBuildingsQueryHandler(IBuildingRepository repo)
    : IRequestHandler<GetBuildingsQuery, IReadOnlyList<BuildingDto>>
{
    public Task<IReadOnlyList<BuildingDto>> Handle(GetBuildingsQuery request, CancellationToken ct)
        => repo.GetBuildingsAsync(request.Search, ct);
}

// ----- Аудитории -----
public sealed record GetRoomsQuery(Guid? BuildingId, string? Search) : IRequest<IReadOnlyList<RoomDto>>;

public sealed class GetRoomsQueryHandler(IBuildingRepository repo)
    : IRequestHandler<GetRoomsQuery, IReadOnlyList<RoomDto>>
{
    public Task<IReadOnlyList<RoomDto>> Handle(GetRoomsQuery request, CancellationToken ct)
        => repo.GetRoomsAsync(request.BuildingId, request.Search, ct);
}
