using Domain.constraints.penalty;

namespace Application.Common.DTO.Management;

/// <summary>Тип оборудования (справочник «Объекты» → «Оборудование»).</summary>
public sealed record EquipmentDto(Guid Id, string Name);

/// <summary>Вес мягкого ограничения (настройка «Ограничения» → веса штрафов).</summary>
public sealed record ConstraintConfigDto(Guid Id, ConstraintType ConstraintType, int Penalty);
