<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { Search, BarChart2, FileText, Users, BookOpen } from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'
import { lookups } from '../api/lookups'
import { workloads } from '../api/workloads'
import { useAsync } from '../composables/useAsync'
import type { InstituteDto, DepartmentDto, TeacherDto, WorkloadItemDto, LessonType } from '../api/types'

// --- Справочники для фильтров ---
const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])

const selectedInstitute = ref('')
const selectedDept = ref('')
const selectedTeacher = ref('')
const searchQuery = ref('')

const isInstituteSelected = computed(() => selectedInstitute.value !== '')

// --- Данные нагрузки ---
const items = ref<WorkloadItemDto[]>([])
const { loading, error, run } = useAsync()
const page = ref(1)
const pageSize = 20
const totalPages = ref(1)
const totalItems = ref(0)

// Сетка недель: максимальная неделя среди строк (минимум 18 для стабильного вида).
const weeks = computed(() => {
  const max = items.value.reduce(
    (m, row) => Math.max(m, ...row.weeklyHours.map(w => w.week), 0), 0)
  return Array.from({ length: Math.max(max, 18) }, (_, i) => i + 1)
})

const totalHours = computed(() => items.value.reduce((sum, r) => sum + r.semesterHours, 0))

const lessonTypeShort: Record<LessonType, string> = {
  Lecture: 'ЛЕК', Seminar: 'СЕМ', Laboratory: 'ЛАБ', Consultation: 'КОНС', Examination: 'ЭКЗ',
}
const hoursOnWeek = (row: WorkloadItemDto, week: number): number =>
  row.weeklyHours.find(w => w.week === week)?.hours ?? 0

async function loadWorkloads() {
  // useAsync отменяет предыдущий запрос (AbortController) и ведёт loading/error.
  await run(async (signal) => {
    const result = await workloads.list({
      instituteId: selectedInstitute.value || undefined,
      departmentId: selectedDept.value || undefined,
      teacherId: selectedTeacher.value || undefined,
      subjectSearch: searchQuery.value.trim() || undefined,
      page: page.value,
      pageSize,
    }, signal)
    items.value = result.items
    totalPages.value = result.totalPages
    totalItems.value = result.totalItems
  })
  if (error.value) items.value = []
}

// Каскад: при смене института грузим его кафедры/преподавателей и сбрасываем вложенные фильтры.
watch(selectedInstitute, async (id) => {
  selectedDept.value = ''
  selectedTeacher.value = ''
  departments.value = []
  teachers.value = []
  if (id) {
    [departments.value, teachers.value] = await Promise.all([
      lookups.departments(id),
      lookups.teachers({ instituteId: id }),
    ])
  }
})

// Любая смена фильтра — на первую страницу и перезагрузка (поиск с задержкой).
let searchTimer: ReturnType<typeof setTimeout> | undefined
watch([selectedInstitute, selectedDept, selectedTeacher], () => { page.value = 1; loadWorkloads() })
watch(searchQuery, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { page.value = 1; loadWorkloads() }, 350)
})
watch(page, loadWorkloads)

function goToPage(p: number) {
  if (p >= 1 && p <= totalPages.value) page.value = p
}

onMounted(async () => {
  institutes.value = await lookups.institutes().catch(() => [])
  await loadWorkloads()
})
</script>

<template>
  <div class="load-container">

    <header class="load-header">
      <div class="title-group">
        <BarChart2 :size="24" color="#1a4d9c" />
        <h2>Нагрузка (ММИС)</h2>
      </div>
      <div class="total-hours-badge">
        <FileText :size="16" /> Всего часов на странице: {{ totalHours }}
      </div>
    </header>

    <div class="filters-bar">
      <div class="filter-group">
        <label>Институт</label>
        <select v-model="selectedInstitute" class="select-dropdown">
          <option value="">Все институты</option>
          <option v-for="inst in institutes" :key="inst.id" :value="inst.id">{{ inst.name }}</option>
        </select>
      </div>

      <div class="filter-group">
        <label>Кафедра</label>
        <select v-model="selectedDept" class="select-dropdown" :disabled="!isInstituteSelected">
          <option value="">Все кафедры</option>
          <option v-for="dept in departments" :key="dept.id" :value="dept.id">{{ dept.name }}</option>
        </select>
      </div>

      <div class="filter-group">
        <label>Преподаватель</label>
        <select v-model="selectedTeacher" class="select-dropdown" :disabled="!isInstituteSelected">
          <option value="">Все преподаватели</option>
          <option v-for="t in teachers" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
      </div>

      <div class="filter-group search-group">
        <label>Поиск по предмету</label>
        <BaseInput v-model="searchQuery" placeholder="Название дисциплины...">
          <template #left-icon><Search :size="16" /></template>
        </BaseInput>
      </div>
    </div>

    <div class="table-wrapper">
      <div v-if="loading" class="state-msg">Загрузка…</div>
      <div v-else-if="error" class="state-msg state-error">{{ error }}</div>
      <div v-else-if="items.length === 0" class="state-msg">Нет данных по выбранным фильтрам.</div>

      <table v-else class="load-table">
        <thead>
          <tr>
            <th class="main-th">Преподаватель / Предмет</th>
            <th>Группа</th>
            <th>Тип</th>
            <th class="total-th">Всего</th>
            <th v-for="w in weeks" :key="w" class="week-th">{{ w }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in items" :key="row.curriculumId" class="data-row">

            <td class="main-td">
              <div class="teacher-name">{{ row.teacher }}</div>
              <div class="subject-name">
                <BookOpen :size="14" /> {{ row.subject }}
              </div>
            </td>

            <td>
              <div class="group-cell">
                <Users :size="14" color="#94a3b8" /> {{ row.group }}
              </div>
            </td>

            <td>
              <span class="type-badge" :class="row.lessonType === 'Lecture' ? 'badge-lek' : 'badge-prak'">
                {{ lessonTypeShort[row.lessonType] }}
              </span>
            </td>

            <td class="total-td"><b>{{ row.semesterHours }}</b></td>

            <td v-for="w in weeks" :key="w" class="week-td">
              {{ hoursOnWeek(row, w) || '' }}
            </td>

          </tr>
        </tbody>
      </table>

      <div v-if="!loading && !error && totalPages > 1" class="pager">
        <button class="page-btn" :disabled="page <= 1" @click="goToPage(page - 1)">Назад</button>
        <span class="page-text">Стр. {{ page }} / {{ totalPages }} · всего {{ totalItems }}</span>
        <button class="page-btn" :disabled="page >= totalPages" @click="goToPage(page + 1)">Вперёд</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.load-container {
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  min-height: calc(100vh - 60px);
  font-family: sans-serif;
}

/* Шапка */
.load-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
}

.title-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

h2 { font-size: 20px; color: #1e293b; margin: 0; }

.total-hours-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #1e293b;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
}

/* Фильтры */
.filters-bar {
  display: flex;
  gap: 20px;
  padding: 0 24px 20px 24px;
  border-bottom: 1px solid #f1f5f9;
}

.filter-group { display: flex; flex-direction: column; gap: 8px; flex: 1; }
.search-group { flex: 1.5; }

label { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; }

.select-dropdown {
  height: 46px;
  padding: 0 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background-color: white;
  font-size: 14px;
  outline: none;
  cursor: pointer;
}

.select-dropdown:disabled { background-color: #f8fafc; cursor: not-allowed; color: #94a3b8; }

/* Таблица */
.table-wrapper {
  overflow-x: auto;
  padding: 0 24px 24px 24px;
}

.state-msg { padding: 40px 0; text-align: center; color: #64748b; font-size: 14px; }
.state-error { color: #dc2626; }

.load-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  margin-top: 16px;
}

.load-table th {
  color: #64748b;
  font-weight: 600;
  padding: 12px 8px;
  border-bottom: 1px solid #e2e8f0;
  text-align: center;
  font-size: 12px;
}

.main-th { text-align: left !important; min-width: 250px; }
.total-th { font-weight: 700; color: #1e293b; }
.week-th { min-width: 36px; }

/* Строки данных */
.data-row td {
  padding: 16px 8px;
  border-bottom: 1px solid #f1f5f9;
  text-align: center;
  vertical-align: middle;
}

.data-row .main-td { text-align: left; }

.teacher-name {
  font-weight: 500;
  color: #1e293b;
  font-size: 13px;
  margin-bottom: 4px;
}

.subject-name {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 12px;
}

.group-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: #475569;
}

.type-badge {
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 700;
}

.badge-lek { background-color: #eff6ff; color: #3b82f6; }
.badge-prak { background-color: #f0fdf4; color: #22c55e; }

.total-td { color: #1e293b; }
.week-td { color: #1a4d9c; font-weight: 500; }

.data-row:hover td { background-color: #f8fafc; }

/* Пагинация */
.pager { display: flex; align-items: center; justify-content: flex-end; gap: 12px; padding-top: 16px; }
.page-text { font-size: 13px; color: #64748b; }
.page-btn {
  background: white; border: 1px solid #cbd5e1; border-radius: 8px;
  padding: 6px 14px; font-size: 13px; color: #475569; cursor: pointer; transition: all 0.2s;
}
.page-btn:hover:not(:disabled) { background-color: #f8fafc; border-color: #94a3b8; }
.page-btn:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
