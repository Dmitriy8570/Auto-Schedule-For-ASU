using Application.Common.DTO.Lookups;
using Application.Common.DTO.Management;

namespace Application.Common.Interfaces;

/// <summary>
/// Управление объектами системы (вкладка «Ограничения» → «Объекты»):
/// CRUD аудиторий и оборудования, а также настройка весов мягких ограничений.
/// Методы самодостаточны (сохраняют изменения сами).
/// </summary>
public interface IManagementRepository
{
    // ----- Аудитории -----
    Task<IReadOnlyList<RoomDto>> GetClassroomsAsync(Guid? buildingId, CancellationToken ct);
    Task<RoomDto> CreateClassroomAsync(string name, int capacity, Guid buildingId, CancellationToken ct);
    Task<RoomDto?> UpdateClassroomAsync(Guid id, string name, int capacity, Guid buildingId, CancellationToken ct);
    Task<bool> DeleteClassroomAsync(Guid id, CancellationToken ct);

    // ----- Оборудование -----
    Task<IReadOnlyList<EquipmentDto>> GetEquipmentsAsync(CancellationToken ct);
    Task<EquipmentDto> CreateEquipmentAsync(string name, CancellationToken ct);
    Task<EquipmentDto?> UpdateEquipmentAsync(Guid id, string name, CancellationToken ct);
    Task<bool> DeleteEquipmentAsync(Guid id, CancellationToken ct);

    // ----- Веса мягких ограничений -----
    Task<IReadOnlyList<ConstraintConfigDto>> GetConstraintsAsync(CancellationToken ct);
    Task<ConstraintConfigDto?> UpdateConstraintPenaltyAsync(Guid id, int penalty, CancellationToken ct);
}
