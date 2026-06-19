using Application.Common.DTO.Management;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Management;

// ----- Список весов мягких ограничений -----
public sealed record GetConstraintsQuery : IRequest<IReadOnlyList<ConstraintConfigDto>>;

public sealed class GetConstraintsQueryHandler(IManagementRepository repo)
    : IRequestHandler<GetConstraintsQuery, IReadOnlyList<ConstraintConfigDto>>
{
    public Task<IReadOnlyList<ConstraintConfigDto>> Handle(GetConstraintsQuery request, CancellationToken ct)
        => repo.GetConstraintsAsync(ct);
}

// ----- Изменение веса -----
public sealed record UpdateConstraintCommand(Guid Id, int Penalty) : IRequest<ConstraintConfigDto>;

public sealed class UpdateConstraintCommandValidator : AbstractValidator<UpdateConstraintCommand>
{
    public UpdateConstraintCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Penalty).GreaterThanOrEqualTo(0).WithMessage("Вес штрафа не может быть отрицательным.");
    }
}

public sealed class UpdateConstraintCommandHandler(IManagementRepository repo)
    : IRequestHandler<UpdateConstraintCommand, ConstraintConfigDto>
{
    public async Task<ConstraintConfigDto> Handle(UpdateConstraintCommand request, CancellationToken ct)
        => await repo.UpdateConstraintPenaltyAsync(request.Id, request.Penalty, ct)
           ?? throw new KeyNotFoundException();
}
