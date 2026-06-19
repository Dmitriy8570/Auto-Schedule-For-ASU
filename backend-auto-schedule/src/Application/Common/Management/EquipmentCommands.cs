using Application.Common.DTO.Management;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Management;

// ----- Список оборудования -----
public sealed record GetEquipmentsQuery : IRequest<IReadOnlyList<EquipmentDto>>;

public sealed class GetEquipmentsQueryHandler(IManagementRepository repo)
    : IRequestHandler<GetEquipmentsQuery, IReadOnlyList<EquipmentDto>>
{
    public Task<IReadOnlyList<EquipmentDto>> Handle(GetEquipmentsQuery request, CancellationToken ct)
        => repo.GetEquipmentsAsync(ct);
}

// ----- Создание -----
public sealed record CreateEquipmentCommand(string Name) : IRequest<EquipmentDto>;

public sealed class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
        => RuleFor(x => x.Name).NotEmpty().WithMessage("Название оборудования обязательно.");
}

public sealed class CreateEquipmentCommandHandler(IManagementRepository repo)
    : IRequestHandler<CreateEquipmentCommand, EquipmentDto>
{
    public Task<EquipmentDto> Handle(CreateEquipmentCommand request, CancellationToken ct)
        => repo.CreateEquipmentAsync(request.Name, ct);
}

// ----- Изменение -----
public sealed record UpdateEquipmentCommand(Guid Id, string Name) : IRequest<EquipmentDto>;

public sealed class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название оборудования обязательно.");
    }
}

public sealed class UpdateEquipmentCommandHandler(IManagementRepository repo)
    : IRequestHandler<UpdateEquipmentCommand, EquipmentDto>
{
    public async Task<EquipmentDto> Handle(UpdateEquipmentCommand request, CancellationToken ct)
        => await repo.UpdateEquipmentAsync(request.Id, request.Name, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Удаление -----
public sealed record DeleteEquipmentCommand(Guid Id) : IRequest;

public sealed class DeleteEquipmentCommandHandler(IManagementRepository repo)
    : IRequestHandler<DeleteEquipmentCommand>
{
    public async Task Handle(DeleteEquipmentCommand request, CancellationToken ct)
    {
        if (!await repo.DeleteEquipmentAsync(request.Id, ct))
            throw new KeyNotFoundException();
    }
}
