using Application.Common.Interfaces;
using Domain.university.groups;
using FluentValidation;
using MediatR;

namespace Application.Common.Constraints;

// ----- Изменение смены группы -----
public sealed record SetGroupShiftCommand(Guid GroupId, Shift Shift) : IRequest;

public sealed class SetGroupShiftCommandValidator : AbstractValidator<SetGroupShiftCommand>
{
    public SetGroupShiftCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Shift).IsInEnum().WithMessage("Недопустимое значение смены.");
    }
}

public sealed class SetGroupShiftCommandHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<SetGroupShiftCommand>
{
    public async Task Handle(SetGroupShiftCommand request, CancellationToken ct)
    {
        if (!await repo.SetGroupShiftAsync(request.GroupId, request.Shift, ct))
            throw new KeyNotFoundException();
    }
}
