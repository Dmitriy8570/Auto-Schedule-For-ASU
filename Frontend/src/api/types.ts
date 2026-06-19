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

// --- Управление объектами (вкладка «Ограничения») ---
export interface EquipmentDto { id: string; name: string }

export type ConstraintType =
  | 'TeacherGap' | 'StudentGap' | 'ClassroomAvailability' | 'TeacherAvailability'
export interface ConstraintConfigDto { id: string; constraintType: ConstraintType; penalty: number }

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

// --- Календарь ---
export type WeekType = 'Red' | 'Blue'

export interface SemesterDto {
  id: string
  startDate: string
  endDate: string
  isCurrent: boolean
  weekCount: number
}

export interface WeekDto {
  id: string
  number: number
  weekType: WeekType
  startDate: string
  endDate: string
}

export interface TimeSlotDto {
  id: string
  dayOfWeek: number   // 0 = Пн … 5 = Сб
  number: number      // номер пары
}

export interface CurriculumOptionDto {
  id: string
  subjectName: string
  teacherId: string
  teacherName: string
  lessonType: DomainLessonType
  streamId: string
  groupNames: string
}

// --- Расписание ---
export type ScheduleVersion = 'Current' | 'Draft'
// Доменный тип занятия (как приходит с бэкенда; отличается от LessonType нагрузки лишь именами enum).
export type DomainLessonType =
  | 'Lecture' | 'Seminar' | 'Laboratory' | 'Consultation' | 'Examination'

export interface LessonDTO {
  id: string
  classroomId: string
  timeSlotId: string
  streamId: string
  curriculumId: string | null
  version: ScheduleVersion
  // Обогащённые поля для сетки.
  dayOfWeek: number        // 0 = Пн … 5 = Сб
  pairNumber: number       // номер пары (1..8)
  weekId: string
  weekType: WeekType
  subjectName: string | null
  teacherId: string | null
  teacherName: string | null
  lessonType: DomainLessonType | null
  classroomName: string
  buildingName: string
  groupNames: string
  studentsCount: number
}

export interface GenerateScheduleResult {
  status: string
  lessonsCreated: number
  objectiveValue: number
  wallTimeSeconds: number
}

export interface PublishInstituteScheduleResult { published: number }
