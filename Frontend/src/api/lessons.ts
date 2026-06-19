import { http } from './http'
import type { LessonDTO, GenerateScheduleResult, PublishInstituteScheduleResult } from './types'

export type ScheduleEntity = 'teacher' | 'group' | 'room'

export interface CreateLessonRequest {
  classroomId: string
  timeSlotId: string
  streamId: string
  semesterId: string
  curriculumId?: string
}

export const lessons = {
  byTeacher: (teacherId: string, weekId?: string) =>
    http.get<LessonDTO[]>(`/lessons/by-teacher/${teacherId}`, { weekId }),

  byGroup: (groupId: string, weekId?: string) =>
    http.get<LessonDTO[]>(`/lessons/by-group/${groupId}`, { weekId }),

  byRoom: (classroomId: string, weekId?: string) =>
    http.get<LessonDTO[]>(`/lessons/by-room/${classroomId}`, { weekId }),

  byEntity: (entity: ScheduleEntity, id: string, weekId?: string) => {
    if (entity === 'teacher') return lessons.byTeacher(id, weekId)
    if (entity === 'group') return lessons.byGroup(id, weekId)
    return lessons.byRoom(id, weekId)
  },

  // Создать занятие (черновик). Возвращает GUID нового занятия.
  create: (body: CreateLessonRequest) => http.post<string>('/lessons', body),

  // Удалить занятие.
  remove: (id: string) => http.del<void>(`/lessons/${id}`),

  // Генерация черновика расписания института на семестр.
  generateForInstitute: (semesterId: string, instituteId: string) =>
    http.post<GenerateScheduleResult>(`/lessons/generate/semester/${semesterId}/institute/${instituteId}`),

  // Публикация черновика института (Draft -> Current).
  publishInstitute: (instituteId: string) =>
    http.post<PublishInstituteScheduleResult>(`/lessons/publish/institute/${instituteId}`),
}
