using Application.Common.DTO.Lookups;
using Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Common.Management;

// ----- Список аудиторий -----
public sealed record GetClassroomsQuery(Guid? BuildingId) : IRequest<IReadOnlyList<RoomDto>>;

public sealed class GetClassroomsQueryHandler(IManagementRepository repo)
    : IRequestHandler<GetClassroomsQuery, IReadOnlyList<RoomDto>>
{
    public Task<IReadOnlyList<RoomDto>> Handle(GetClassroomsQuery request, CancellationToken ct)
        => repo.GetClassroomsAsync(request.BuildingId, ct);
}

// ----- Создание -----
public sealed record CreateClassroomCommand(string Name, int Capacity, Guid BuildingId) : IRequest<RoomDto>;

public sealed class CreateClassroomCommandValidator : AbstractValidator<CreateClassroomCommand>
{
    public CreateClassroomCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название аудитории обязательно.");
        RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("Вместимость должна быть положительной.");
        RuleFor(x => x.BuildingId).NotEmpty().WithMessage("Корпус обязателен.");
    }
}

public sealed class CreateClassroomCommandHandler(IManagementRepository repo)
    : IRequestHandler<CreateClassroomCommand, RoomDto>
{
    public Task<RoomDto> Handle(CreateClassroomCommand request, CancellationToken ct)
        => repo.CreateClassroomAsync(request.Name, request.Capacity, request.BuildingId, ct);
}

// ----- Изменение -----
public sealed record UpdateClassroomCommand(Guid Id, string Name, int Capacity, Guid BuildingId) : IRequest<RoomDto>;

public sealed class UpdateClassroomCommandValidator : AbstractValidator<UpdateClassroomCommand>
{
    public UpdateClassroomCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Название аудитории обязательно.");
        RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("Вместимость должна быть положительной.");
        RuleFor(x => x.BuildingId).NotEmpty().WithMessage("Корпус обязателен.");
    }
}

public sealed class UpdateClassroomCommandHandler(IManagementRepository repo)
    : IRequestHandler<UpdateClassroomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(UpdateClassroomCommand request, CancellationToken ct)
        => await repo.UpdateClassroomAsync(request.Id, request.Name, request.Capacity, request.BuildingId, ct)
           ?? throw new KeyNotFoundException();
}

// ----- Удаление -----
public sealed record DeleteClassroomCommand(Guid Id) : IRequest;

public sealed class DeleteClassroomCommandHandler(IManagementRepository repo)
    : IRequestHandler<DeleteClassroomCommand>
{
    public async Task Handle(DeleteClassroomCommand request, CancellationToken ct)
    {
        if (!await repo.DeleteClassroomAsync(request.Id, ct))
            throw new KeyNotFoundException();
    }
}
