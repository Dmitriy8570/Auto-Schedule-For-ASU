import { http } from './http'
import type {
  LessonDTO, GenerateScheduleResult, PublishInstituteScheduleResult, DiscardInstituteScheduleResult,
  GenerationJobStatus, GenerationRunDto,
} from './types'

export type ScheduleEntity = 'teacher' | 'group' | 'room'

export interface CreateLessonRequest {
  classroomId: string
  timeSlotId: string
  streamId: string
  semesterId: string
  curriculumId?: string
}

export interface UpdateLessonRequest {
  classroomId: string
  timeSlotId: string
  streamId: string
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

  // Изменить занятие (аудитория/слот/учебный план) одной командой.
  update: (id: string, body: UpdateLessonRequest) => http.put<void>(`/lessons/${id}`, body),

  // Удалить занятие.
  remove: (id: string) => http.del<void>(`/lessons/${id}`),

  // Генерация черновика расписания института на семестр (синхронная, держит HTTP-поток).
  generateForInstitute: (semesterId: string, instituteId: string) =>
    http.post<GenerateScheduleResult>(`/lessons/generate/semester/${semesterId}/institute/${instituteId}`),

  // Поставить генерацию института в очередь (фоновая) — возвращает статус задачи (202).
  generateForInstituteAsync: (semesterId: string, instituteId: string) =>
    http.post<GenerationJobStatus>(`/lessons/generate/semester/${semesterId}/institute/${instituteId}/async`),

  // Статус фоновой задачи генерации.
  generationStatus: (jobId: string) =>
    http.get<GenerationJobStatus>(`/lessons/generate/status/${jobId}`),

  // История автогенерации (по убыванию времени завершения) с фильтрами.
  generationHistory: (params: { semesterId?: string; instituteId?: string; limit?: number } = {}) =>
    http.get<GenerationRunDto[]>('/lessons/generate/history', { ...params }),

  // Публикация черновика института (Draft -> Current).
  publishInstitute: (instituteId: string) =>
    http.post<PublishInstituteScheduleResult>(`/lessons/publish/institute/${instituteId}`),

  // Сброс черновика института до выгруженного (удалить Draft, оставить Current).
  discardInstitute: (instituteId: string) =>
    http.post<DiscardInstituteScheduleResult>(`/lessons/discard/institute/${instituteId}`),
}
