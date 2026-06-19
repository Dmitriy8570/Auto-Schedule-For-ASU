import { http } from './http'
import type { RoomDto, EquipmentDto, ConstraintConfigDto } from './types'

// Управление объектами системы (вкладка «Ограничения»). См. endpoints.txt, раздел 8.
export const management = {
  // Аудитории
  classrooms: (buildingId?: string) => http.get<RoomDto[]>('/classrooms', { buildingId }),
  createClassroom: (body: { name: string; capacity: number; buildingId: string }) =>
    http.post<RoomDto>('/classrooms', body),
  updateClassroom: (id: string, body: { name: string; capacity: number; buildingId: string }) =>
    http.put<RoomDto>(`/classrooms/${id}`, body),
  deleteClassroom: (id: string) => http.del<void>(`/classrooms/${id}`),

  // Оборудование
  equipments: () => http.get<EquipmentDto[]>('/equipments'),
  createEquipment: (name: string) => http.post<EquipmentDto>('/equipments', { name }),
  updateEquipment: (id: string, name: string) => http.put<EquipmentDto>(`/equipments/${id}`, { name }),
  deleteEquipment: (id: string) => http.del<void>(`/equipments/${id}`),

  // Веса мягких ограничений
  constraints: () => http.get<ConstraintConfigDto[]>('/constraints'),
  updateConstraint: (id: string, penalty: number) =>
    http.put<ConstraintConfigDto>(`/constraints/${id}`, { penalty }),
}
