<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  History, RotateCw, Trash2, PlusCircle, User, Clock, ChevronLeft, ChevronRight, Layers,
} from 'lucide-vue-next'
import { lookups } from '../api/lookups'
import { workloads } from '../api/workloads'
import { ApiError } from '../api/http'
import type { InstituteDto, DepartmentDto, TeacherDto, WorkloadChangeDto, LogAction } from '../api/types'

const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])

const selectedInstitute = ref('')
const selectedDept = ref('')
const selectedTeacher = ref('')

const isInstituteSelected = computed(() => selectedInstitute.value !== '')

// id преподавателя -> имя, чтобы показать ФИО (журнал хранит только id).
const teacherNames = computed<Record<string, string>>(() =>
  Object.fromEntries(teachers.value.map(t => [t.id, t.name])))

const items = ref<WorkloadChangeDto[]>([])
const loading = ref(false)
const error = ref('')

// Пагинация на стороне сервера: страница запрашивается у бэкенда.
const page = ref(1)
const pageSize = 10
const totalItems = ref(0)
const totalPages = ref(1)
const rangeFrom = computed(() => totalItems.value === 0 ? 0 : (page.value - 1) * pageSize + 1)
const rangeTo = computed(() => Math.min(page.value * pageSize, totalItems.value))

const actionMeta: Record<LogAction, { title: string; type: string; icon: typeof PlusCircle }> = {
  Add: { title: 'Добавлена нагрузка', type: 'add', icon: PlusCircle },
  Update: { title: 'Изменена нагрузка', type: 'edit', icon: RotateCw },
  Delete: { title: 'Удалена нагрузка', type: 'delete', icon: Trash2 },
}

const formatTime = (iso: string): string => {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString('ru-RU')
}
const teacherName = (id: string): string => teacherNames.value[id] ?? 'Преподаватель не в выборке'

async function loadChanges() {
  loading.value = true
  error.value = ''
  try {
    const res = await workloads.changes({
      teacherId: selectedTeacher.value || undefined,
      page: page.value,
      pageSize,
    })
    items.value = res.items
    totalItems.value = res.totalItems
    totalPages.value = res.totalPages
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : 'Не удалось загрузить историю.'
    items.value = []
    totalItems.value = 0
    totalPages.value = 1
  } finally {
    loading.value = false
  }
}

// Смена фильтра — вернуться на первую страницу.
function reload() {
  page.value = 1
  loadChanges()
}

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

watch(selectedTeacher, reload)

function goToPage(p: number) {
  if (p >= 1 && p <= totalPages.value && p !== page.value) {
    page.value = p
    loadChanges()
  }
}

onMounted(async () => {
  institutes.value = await lookups.institutes().catch(() => [])
  await loadChanges()
})
</script>

<template>
  <div class="history-container">

    <header class="history-header">
      <div class="title-group">
        <History :size="24" color="#1e293b" />
        <h2>История изменений нагрузки</h2>
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
    </div>

    <div class="history-list-wrapper">
      <div v-if="loading" class="state-msg">Загрузка…</div>
      <div v-else-if="error" class="state-msg state-error">{{ error }}</div>
      <div v-else-if="totalItems === 0" class="state-msg">Изменений не найдено.</div>

      <div v-else class="history-list">
        <div v-for="(event, idx) in items" :key="idx" class="history-card">

          <div class="status-icon-wrapper" :class="`bg-${actionMeta[event.action].type}`">
            <component :is="actionMeta[event.action].icon" :size="20" :class="`icon-${actionMeta[event.action].type}`" />
          </div>

          <div class="card-content">
            <div class="card-title">{{ actionMeta[event.action].title }}</div>
            <div class="card-subtitle">
              Часы: {{ event.oldValue }} → {{ event.newValue }}
              <span class="scope-badge">{{ event.scope === 'Semester' ? 'Семестр' : 'Неделя' }}</span>
            </div>

            <div class="card-meta">
              <div class="meta-item"><User :size="14" /> {{ teacherName(event.teacherId) }}</div>
              <div class="meta-item"><Layers :size="14" /> {{ event.scope === 'Semester' ? 'Семестровая' : 'Понедельная' }}</div>
              <div class="meta-item"><Clock :size="14" /> {{ formatTime(event.timeStamp) }}</div>
            </div>
          </div>

        </div>
      </div>
    </div>

    <div v-if="!loading && !error && totalItems > 0" class="pagination-bar">
      <div class="pagination-info">Показано {{ rangeFrom }}–{{ rangeTo }} из {{ totalItems }}</div>
      <div class="pagination-controls">
        <button class="page-btn" :disabled="page <= 1" @click="goToPage(page - 1)"><ChevronLeft :size="18" /></button>
        <div class="page-text">{{ page }} / {{ totalPages }}</div>
        <button class="page-btn" :disabled="page >= totalPages" @click="goToPage(page + 1)"><ChevronRight :size="18" /></button>
      </div>
    </div>

  </div>
</template>

<style scoped>
.history-container {
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  height: calc(100vh - 60px);
  font-family: sans-serif;
}

.history-header {
  padding: 20px 24px;
  border-bottom: 1px solid #f1f5f9;
}

.title-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

h2 { font-size: 20px; color: #1e293b; margin: 0; }

.filters-bar {
  display: flex;
  gap: 20px;
  padding: 20px 24px;
  border-bottom: 1px solid #f1f5f9;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

label { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.025em; }

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

.select-dropdown:disabled {
  background-color: #f8fafc;
  cursor: not-allowed;
  color: #94a3b8;
}

.history-list-wrapper {
  flex-grow: 1;
  overflow-y: auto;
  padding: 24px;
  background-color: #f8fafc;
}

.state-msg { padding: 40px 0; text-align: center; color: #64748b; font-size: 14px; }
.state-error { color: #dc2626; }

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-card {
  background-color: white;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: flex-start;
  gap: 16px;
  transition: box-shadow 0.2s;
}

.history-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.status-icon-wrapper {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.bg-add { background-color: #f0fdf4; }
.icon-add { color: #16a34a; }
.bg-delete { background-color: #fef2f2; }
.icon-delete { color: #dc2626; }
.bg-edit { background-color: #eff6ff; }
.icon-edit { color: #2563eb; }

.card-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.card-title {
  font-weight: 600;
  color: #1e293b;
  font-size: 15px;
}

.card-subtitle {
  color: #475569;
  font-size: 14px;
  margin-bottom: 6px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.scope-badge {
  background-color: #f1f5f9;
  color: #64748b;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.card-meta {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 13px;
}

.pagination-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background-color: white;
  border-top: 1px solid #f1f5f9;
  border-radius: 0 0 16px 16px;
}

.pagination-info {
  color: #64748b;
  font-size: 13px;
}

.pagination-controls {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-btn {
  background: white;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s;
}

.page-btn:hover:not(:disabled) {
  background-color: #f8fafc;
  border-color: #94a3b8;
}

.page-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.page-text {
  font-size: 14px;
  font-weight: 500;
  color: #1e293b;
}
</style>
