// Типы, повторяющие DTO бэкенда. Имена полей — camelCase (System.Text.Json),
// enum'ы приходят строками (JsonStringEnumConverter на бэке).

export interface LoginResponse {
  token: string
  expiresAtUtc: string
  username: string
  displayName: string
  groups: string[]
}

// --- Справочники (каскадные фильтры) ---
export type TypeDegree =
  | 'Secondary' | 'Bachelor' | 'Specialist' | 'Master' | 'Postgraduate' | 'Doctoral'
export type Shift = 'First' | 'Second' | 'Evening'

export interface InstituteDto { id: string; name: string }
export interface DegreeDto { id: string; typeDegree: TypeDegree; instituteId: string }
export interface CourseDto { id: string; number: number; degreeId: string }
export interface GroupDto {
  id: string; name: string; shift: Shift; studentCount: number; courseId: string; courseNumber: number
}
export interface DepartmentDto { id: string; name: string; instituteId: string }
export interface TeacherDto { id: string; name: string; departmentId: string; departmentName: string }
export interface BuildingDto { id: string; name: string }
export interface RoomDto { id: string; name: string; capacity: number; buildingId: string; buildingName: string }

// --- Нагрузка ---
export type LessonType = 'Lecture' | 'Seminar' | 'Laboratory' | 'Consultation' | 'Examination'

export interface WeekHoursDto { week: number; hours: number }
export interface WorkloadItemDto {
  curriculumId: string
  teacher: string
  subject: string
  group: string
  lessonType: LessonType
  semesterHours: number
  weeklyHours: WeekHoursDto[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type LogAction = 'Add' | 'Update' | 'Delete'
export interface WorkloadChangeDto {
  action: LogAction
  oldValue: number
  newValue: number
  timeStamp: string
  scope: 'Semester' | 'Week'
  curriculumId: string
  teacherId: string
  subjectId: string
  streamId: string
}

// --- Расписание ---
export type ScheduleVersion = 'Current' | 'Draft'
export interface LessonDTO {
  id: string
  classroomId: string
  timeSlotId: string
  streamId: string
  version: ScheduleVersion
}

export interface GenerateScheduleResult {
  status: string
  lessonsCreated: number
  objectiveValue: number
  wallTimeSeconds: number
}

export interface PublishInstituteScheduleResult { published: number }
