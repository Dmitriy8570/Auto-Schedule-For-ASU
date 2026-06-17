namespace Application.Common.DTO.Lookups;

/// <summary>Учебный корпус.</summary>
public sealed record BuildingDto(Guid Id, string Name);

/// <summary>Аудитория — конечный выбор для занятости аудитории.</summary>
public sealed record RoomDto(Guid Id, string Name, int Capacity, Guid BuildingId, string BuildingName);
