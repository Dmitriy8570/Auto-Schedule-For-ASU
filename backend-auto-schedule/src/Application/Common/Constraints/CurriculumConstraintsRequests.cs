using Application.Common.DTO.Constraints;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Constraints;

// ----- Чтение по-нагрузочных ограничений учебного плана -----
public sealed record GetCurriculumConstraintsQuery(Guid CurriculumId) : IRequest<CurriculumConstraintsDto>;

public sealed class GetCurriculumConstraintsQueryHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<GetCurriculumConstraintsQuery, CurriculumConstraintsDto>
{
    public async Task<CurriculumConstraintsDto> Handle(GetCurriculumConstraintsQuery request, CancellationToken ct)
        => await repo.GetCurriculumConstraintsAsync(request.CurriculumId, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Изменение по-нагрузочных ограничений учебного плана -----
public sealed record SetCurriculumConstraintsCommand(Guid CurriculumId, CurriculumConstraintsDto Constraints) : IRequest;

public sealed class SetCurriculumConstraintsCommandValidator : AbstractValidator<SetCurriculumConstraintsCommand>
{
    public SetCurriculumConstraintsCommandValidator()
    {
        RuleFor(x => x.CurriculumId).NotEmpty();
        RuleFor(x => x.Constraints).NotNull();
        RuleFor(x => x.Constraints.RequiredEquipmentIds).NotNull();
        RuleForEach(x => x.Constraints.RequiredEquipmentIds).NotEmpty()
            .WithMessage("Идентификатор оборудования не может быть пустым.");
    }
}

public sealed class SetCurriculumConstraintsCommandHandler(IConstraintConfigurationRepository repo)
    : IRequestHandler<SetCurriculumConstraintsCommand>
{
    public async Task Handle(SetCurriculumConstraintsCommand request, CancellationToken ct)
    {
        if (!await repo.SetCurriculumConstraintsAsync(request.CurriculumId, request.Constraints, ct))
            throw new KeyNotFoundException();
    }
}
