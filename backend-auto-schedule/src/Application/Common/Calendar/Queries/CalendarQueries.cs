using Application.Common.DTO.Calendar;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Calendar.Queries;

// ----- Семестры -----
public sealed record GetSemestersQuery : IRequest<IReadOnlyList<SemesterDto>>;

public sealed class GetSemestersQueryHandler(ICalendarRepository repo)
    : IRequestHandler<GetSemestersQuery, IReadOnlyList<SemesterDto>>
{
    public Task<IReadOnlyList<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken ct)
        => repo.GetSemestersAsync(ct);
}

// ----- Недели семестра -----
public sealed record GetWeeksQuery(Guid SemesterId) : IRequest<IReadOnlyList<WeekDto>>;

public sealed class GetWeeksQueryHandler(ICalendarRepository repo)
    : IRequestHandler<GetWeeksQuery, IReadOnlyList<WeekDto>>
{
    public Task<IReadOnlyList<WeekDto>> Handle(GetWeeksQuery request, CancellationToken ct)
        => repo.GetWeeksAsync(request.SemesterId, ct);
}
