import { http } from './http'
import type { PagedResult, WorkloadItemDto, WorkloadChangeDto } from './types'

export interface WorkloadsFilter {
  instituteId?: string
  departmentId?: string
  teacherId?: string
  subjectSearch?: string
  page?: number
  pageSize?: number
}

export interface WorkloadChangesFilter {
  teacherId?: string
  groupId?: string
  subjectId?: string
  semesterId?: string
  from?: string
  to?: string
}

export const workloads = {
  // Текущая нагрузка (учебный план) с пагинацией.
  list: (filter: WorkloadsFilter = {}) =>
    http.get<PagedResult<WorkloadItemDto>>('/workloads', { ...filter }),

  // Журнал изменений нагрузки.
  changes: (filter: WorkloadChangesFilter = {}) =>
    http.get<WorkloadChangeDto[]>('/workloads/changes', { ...filter }),
}
