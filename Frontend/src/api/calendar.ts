import { http } from './http'
import type { SemesterDto, WeekDto, TimeSlotDto, CurriculumOptionDto } from './types'

// Календарь и справочники редактора расписания. См. endpoints.txt, раздел 7.
export const calendar = {
  semesters: () => http.get<SemesterDto[]>('/semesters'),

  weeks: (semesterId: string) => http.get<WeekDto[]>('/weeks', { semesterId }),

  timeslots: (weekId: string) => http.get<TimeSlotDto[]>('/timeslots', { weekId }),

  curriculums: (params: { groupId?: string; teacherId?: string; semesterId?: string } = {}) =>
    http.get<CurriculumOptionDto[]>('/curriculums', params),
}
