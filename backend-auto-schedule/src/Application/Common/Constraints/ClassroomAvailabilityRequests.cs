using Application.Common.DTO.Constraints;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Constraints;

// ----- Чтение сетки доступности аудитории -----
public sealed record GetClassroomAvailabilityQuery(Guid ClassroomId) : IRequest<IReadOnlyList<AvailabilityCellDto>>;

public sealed class GetClassroomAvailabilityQueryHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<GetClassroomAvailabilityQuery, IReadOnlyList<AvailabilityCellDto>>
{
    public async Task<IReadOnlyList<AvailabilityCellDto>> Handle(GetClassroomAvailabilityQuery request, CancellationToken ct)
        => await repo.GetClassroomAvailabilityAsync(request.ClassroomId, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Полная замена сетки доступности аудитории -----
public sealed record SetClassroomAvailabilityCommand(Guid ClassroomId, IReadOnlyList<AvailabilityCellDto> Cells) : IRequest;

public sealed class SetClassroomAvailabilityCommandValidator : AbstractValidator<SetClassroomAvailabilityCommand>
{
    public SetClassroomAvailabilityCommandValidator()
    {
        RuleFor(x => x.ClassroomId).NotEmpty();
        RuleForEach(x => x.Cells).SetValidator(new AvailabilityCellValidator());
    }
}

public sealed class SetClassroomAvailabilityCommandHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<SetClassroomAvailabilityCommand>
{
    public async Task Handle(SetClassroomAvailabilityCommand request, CancellationToken ct)
    {
        if (!await repo.SetClassroomAvailabilityAsync(request.ClassroomId, request.Cells, ct))
            throw new KeyNotFoundException();
    }
}
