using Application.Common.DTO.Schedule;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Schedule.Queries;

// ----- Временные слоты недели -----
public sealed record GetTimeSlotsQuery(Guid WeekId) : IRequest<IReadOnlyList<TimeSlotDto>>;

public sealed class GetTimeSlotsQueryHandler(IScheduleLookupRepository repo)
    : IRequestHandler<GetTimeSlotsQuery, IReadOnlyList<TimeSlotDto>>
{
    public Task<IReadOnlyList<TimeSlotDto>> Handle(GetTimeSlotsQuery request, CancellationToken ct)
        => repo.GetTimeSlotsAsync(request.WeekId, ct);
}

// ----- Учебные планы (для формы добавления пары) -----
public sealed record GetCurriculumsQuery(Guid? GroupId, Guid? TeacherId, Guid? SemesterId)
    : IRequest<IReadOnlyList<CurriculumOptionDto>>;

public sealed class GetCurriculumsQueryHandler(IScheduleLookupRepository repo)
    : IRequestHandler<GetCurriculumsQuery, IReadOnlyList<CurriculumOptionDto>>
{
    public Task<IReadOnlyList<CurriculumOptionDto>> Handle(GetCurriculumsQuery request, CancellationToken ct)
        => repo.GetCurriculumsAsync(request.GroupId, request.TeacherId, request.SemesterId, ct);
}
