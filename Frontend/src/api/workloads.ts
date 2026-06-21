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
  page?: number
  pageSize?: number
}

export const workloads = {
  // Текущая нагрузка (учебный план) с пагинацией.
  list: (filter: WorkloadsFilter = {}, signal?: AbortSignal) =>
    http.get<PagedResult<WorkloadItemDto>>('/workloads', { ...filter }, signal),

  // Журнал изменений нагрузки с пагинацией.
  changes: (filter: WorkloadChangesFilter = {}) =>
    http.get<PagedResult<WorkloadChangeDto>>('/workloads/changes', { ...filter }),
}
