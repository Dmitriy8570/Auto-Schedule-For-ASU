import { http } from './http'
import type { AvailabilityCellDto, CurriculumConstraintsDto, Shift } from './types'

// Конфигурация ограничений солвера (вкладка «Ограничения», правые панели).
// PUT доступности — полная замена сетки; нейтральные ячейки можно не передавать.
export const constraints = {
  // Доступность преподавателя
  teacherAvailability: (teacherId: string) =>
    http.get<AvailabilityCellDto[]>(`/availability/teacher/${teacherId}`),
  setTeacherAvailability: (teacherId: string, cells: AvailabilityCellDto[]) =>
    http.put<void>(`/availability/teacher/${teacherId}`, cells),

  // Доступность аудитории
  classroomAvailability: (classroomId: string) =>
    http.get<AvailabilityCellDto[]>(`/availability/classroom/${classroomId}`),
  setClassroomAvailability: (classroomId: string, cells: AvailabilityCellDto[]) =>
    http.put<void>(`/availability/classroom/${classroomId}`, cells),

  // Оснащение аудитории оборудованием (идентификаторы оборудования)
  classroomEquipment: (classroomId: string) =>
    http.get<string[]>(`/classrooms/${classroomId}/equipment`),
  setClassroomEquipment: (classroomId: string, equipmentIds: string[]) =>
    http.put<void>(`/classrooms/${classroomId}/equipment`, equipmentIds),

  // По-нагрузочные ограничения учебного плана
  curriculumConstraints: (curriculumId: string) =>
    http.get<CurriculumConstraintsDto>(`/curriculums/${curriculumId}/constraints`),
  setCurriculumConstraints: (curriculumId: string, body: CurriculumConstraintsDto) =>
    http.put<void>(`/curriculums/${curriculumId}/constraints`, body),

  // Смена группы
  setGroupShift: (groupId: string, shift: Shift) =>
    http.put<void>(`/groups/${groupId}/shift`, { shift }),
}
