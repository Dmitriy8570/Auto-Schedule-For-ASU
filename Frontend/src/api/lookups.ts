import { http } from './http'
import type {
  InstituteDto, DegreeDto, CourseDto, GroupDto, DepartmentDto, TeacherDto, BuildingDto, RoomDto,
} from './types'

// Справочники для каскадных фильтров (см. endpoints.txt, раздел 6).
export const lookups = {
  institutes: (search?: string) =>
    http.get<InstituteDto[]>('/institutes', { search }),

  degrees: (instituteId?: string) =>
    http.get<DegreeDto[]>('/degrees', { instituteId }),

  courses: (degreeId?: string, instituteId?: string) =>
    http.get<CourseDto[]>('/courses', { degreeId, instituteId }),

  groups: (params: { courseId?: string; degreeId?: string; instituteId?: string; search?: string } = {}) =>
    http.get<GroupDto[]>('/groups', params),

  departments: (instituteId?: string, search?: string) =>
    http.get<DepartmentDto[]>('/departments', { instituteId, search }),

  teachers: (params: { instituteId?: string; departmentId?: string; search?: string } = {}) =>
    http.get<TeacherDto[]>('/teachers', params),

  buildings: (search?: string) =>
    http.get<BuildingDto[]>('/buildings', { search }),

  rooms: (buildingId?: string, search?: string) =>
    http.get<RoomDto[]>('/rooms', { buildingId, search }),
}
