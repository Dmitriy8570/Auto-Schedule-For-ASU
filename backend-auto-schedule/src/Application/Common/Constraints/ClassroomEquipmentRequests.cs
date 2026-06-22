using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Constraints;

// ----- Чтение оснащения аудитории оборудованием -----
public sealed record GetClassroomEquipmentQuery(Guid ClassroomId) : IRequest<IReadOnlyList<Guid>>;

public sealed class GetClassroomEquipmentQueryHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<GetClassroomEquipmentQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GetClassroomEquipmentQuery request, CancellationToken ct)
        => await repo.GetClassroomEquipmentAsync(request.ClassroomId, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Изменение оснащения аудитории оборудованием -----
public sealed record SetClassroomEquipmentCommand(Guid ClassroomId, IReadOnlyList<Guid> EquipmentIds) : IRequest;

public sealed class SetClassroomEquipmentCommandValidator : AbstractValidator<SetClassroomEquipmentCommand>
{
    public SetClassroomEquipmentCommandValidator()
    {
        RuleFor(x => x.ClassroomId).NotEmpty();
        RuleFor(x => x.EquipmentIds).NotNull();
        RuleForEach(x => x.EquipmentIds).NotEmpty()
            .WithMessage("Идентификатор оборудования не может быть пустым.");
    }
}

public sealed class SetClassroomEquipmentCommandHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<SetClassroomEquipmentCommand>
{
    public async Task Handle(SetClassroomEquipmentCommand request, CancellationToken ct)
    {
        if (!await repo.SetClassroomEquipmentAsync(request.ClassroomId, request.EquipmentIds, ct))
            throw new KeyNotFoundException();
    }
}
