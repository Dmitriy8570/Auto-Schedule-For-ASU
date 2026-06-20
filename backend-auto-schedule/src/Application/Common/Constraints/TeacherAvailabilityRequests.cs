using Application.Common.DTO.Constraints;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Constraints;

// ----- Чтение сетки доступности преподавателя -----
public sealed record GetTeacherAvailabilityQuery(Guid TeacherId) : IRequest<IReadOnlyList<AvailabilityCellDto>>;

public sealed class GetTeacherAvailabilityQueryHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<GetTeacherAvailabilityQuery, IReadOnlyList<AvailabilityCellDto>>
{
    public async Task<IReadOnlyList<AvailabilityCellDto>> Handle(GetTeacherAvailabilityQuery request, CancellationToken ct)
        => await repo.GetTeacherAvailabilityAsync(request.TeacherId, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Полная замена сетки доступности преподавателя -----
public sealed record SetTeacherAvailabilityCommand(Guid TeacherId, IReadOnlyList<AvailabilityCellDto> Cells) : IRequest;

public sealed class SetTeacherAvailabilityCommandValidator : AbstractValidator<SetTeacherAvailabilityCommand>
{
    public SetTeacherAvailabilityCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleForEach(x => x.Cells).SetValidator(new AvailabilityCellValidator());
    }
}

public sealed class SetTeacherAvailabilityCommandHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<SetTeacherAvailabilityCommand>
{
    public async Task Handle(SetTeacherAvailabilityCommand request, CancellationToken ct)
    {
        if (!await repo.SetTeacherAvailabilityAsync(request.TeacherId, request.Cells, ct))
            throw new KeyNotFoundException();
    }
}
