<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { AlertTriangle, Users, BookOpen, Layers } from 'lucide-vue-next'
import BaseSelect, { type SelectOption } from './BaseSelect.vue'
import { lookups } from '../api/lookups'
import { workloads } from '../api/workloads'
import { useAsync } from '../composables/useAsync'
import { useLookupsStore } from '../stores/lookups'
import { useRealtimeStore } from '../stores/realtime'
import type {
  InstituteDto, DepartmentDto, TeacherDto, SemesterDto, UnplacedWorkloadRow, LessonType,
} from '../api/types'

const lookupsStore = useLookupsStore()
const realtime = useRealtimeStore()

const semesters = ref<SemesterDto[]>([])
const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])

const selSemester = ref('')
const selInstitute = ref('')
const selDept = ref('')
const selTeacher = ref('')

const rows = ref<UnplacedWorkloadRow[]>([])
const { loading, error, run } = useAsync()

const lessonTypeShort: Record<LessonType, string> = {
  Lecture: 'ЛЕК', Seminar: 'СЕМ', Laboratory: 'ЛАБ', Consultation: 'КОНС', Examination: 'ЭКЗ',
}

// Опции для выпадающих списков с поиском (BaseSelect). Все доступны сразу; списки сужаются от
// выбранного «родителя», но не блокируются — как в просмотре расписания.
const semesterOptions = computed<SelectOption[]>(() => semesters.value.map(s => ({
  value: s.id, label: `${s.startDate} — ${s.endDate}${s.isCurrent ? ' (текущий)' : ''}`,
})))
const instituteOptions = computed<SelectOption[]>(() => institutes.value.map(i => ({ value: i.id, label: i.name })))
const departmentOptions = computed<SelectOption[]>(() => departments.value.map(d => ({ value: d.id, label: d.name })))
const teacherOptions = computed<SelectOption[]>(() =>
  teachers.value.map(t => ({ value: t.id, label: t.name, sublabel: t.departmentName })))

// Сводка: всего недоразмещённых пар и сколько преподавателей затронуто.
const totalUnplaced = computed(() => rows.value.reduce((s, r) => s + r.unplacedPairs, 0))
const teachersAffected = computed(() => new Set(rows.value.map(r => r.teacher)).size)

async function load() {
  if (!selSemester.value) { rows.value = []; return }
  await run(async (signal) => {
    rows.value = await workloads.unplaced({
      semesterId: selSemester.value,
      instituteId: selInstitute.value || undefined,
      departmentId: selDept.value || undefined,
      teacherId: selTeacher.value || undefined,
    }, signal)
  })
  if (error.value) rows.value = []
}

// Каскад: при смене института сужаем кафедры/преподавателей (или возвращаем полный список, если
// институт сброшен) и обнуляем вложенные выборы. Списки не блокируются — доступны сразу.
watch(selInstitute, async (id) => {
  selDept.value = ''
  selTeacher.value = ''
  ;[departments.value, teachers.value] = await Promise.all([
    lookups.departments(id || undefined).catch(() => []),
    lookups.teachers({ instituteId: id || undefined }).catch(() => []),
  ])
})

// При смене кафедры сужаем преподавателей (в рамках выбранного института, если он задан).
watch(selDept, async (id) => {
  selTeacher.value = ''
  teachers.value = await lookups.teachers({
    instituteId: selInstitute.value || undefined, departmentId: id || undefined,
  }).catch(() => [])
})

watch([selSemester, selInstitute, selDept, selTeacher], load)

// Реальное время: размещение/сброс занятий меняет дефицит — перечитываем при событии.
watch(() => realtime.scheduleTick, load)

onMounted(async () => {
  // Семестры/институты — из кэша стора; кафедры/преподаватели грузим полностью.
  semesters.value = await lookupsStore.ensureSemesters()
  institutes.value = await lookupsStore.ensureInstitutes()
  ;[departments.value, teachers.value] = await Promise.all([
    lookups.departments().catch(() => []),
    lookups.teachers().catch(() => []),
  ])
  const cur = semesters.value.find(s => s.isCurrent)
  selSemester.value = cur?.id ?? semesters.value[0]?.id ?? ''
  await load()
})
</script>

<template>
  <div class="ul-container">
    <header class="ul-header">
      <div class="title-group">
        <AlertTriangle :size="24" color="#b45309" />
        <h2>Нераспределённая нагрузка</h2>
      </div>
      <div class="ul-summary" v-if="rows.length">
        <span class="ul-chip ul-chip-amber"><Layers :size="14" /> Пар не размещено: {{ totalUnplaced }}</span>
        <span class="ul-chip"><Users :size="14" /> Преподавателей: {{ teachersAffected }}</span>
      </div>
    </header>

    <div class="filters-bar">
      <div class="filter-group">
        <label>Семестр</label>
        <BaseSelect v-model="selSemester" :options="semesterOptions"
                    placeholder="Выберите семестр…" search-placeholder="Поиск семестра…" />
      </div>
      <div class="filter-group">
        <label>Институт</label>
        <BaseSelect v-model="selInstitute" :options="instituteOptions"
                    placeholder="Все институты" clear-label="Все институты" search-placeholder="Поиск института…" />
      </div>
      <div class="filter-group">
        <label>Кафедра</label>
        <BaseSelect v-model="selDept" :options="departmentOptions"
                    placeholder="Все кафедры" clear-label="Все кафедры" search-placeholder="Поиск кафедры…" />
      </div>
      <div class="filter-group">
        <label>Преподаватель</label>
        <BaseSelect v-model="selTeacher" :options="teacherOptions"
                    placeholder="Все преподаватели" clear-label="Все преподаватели" search-placeholder="Поиск преподавателя…" />
      </div>
    </div>

    <div class="table-wrapper">
      <div v-if="!selSemester" class="state-msg">Выберите семестр для расчёта.</div>
      <div v-else-if="loading" class="state-msg">Загрузка…</div>
      <div v-else-if="error" class="state-msg state-error">{{ error }}</div>
      <div v-else-if="rows.length === 0" class="state-msg state-ok">
        Вся нагрузка распределена — нераспределённых пар нет.
      </div>

      <table v-else class="ul-table">
        <thead>
          <tr>
            <th class="main-th">Преподаватель / Предмет</th>
            <th>Группа</th>
            <th>Тип</th>
            <th class="ul-num">План</th>
            <th class="ul-num">Размещено</th>
            <th class="ul-num">Не размещено</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rows" :key="r.curriculumId" class="data-row">
            <td class="main-td">
              <div class="teacher-name">{{ r.teacher }}</div>
              <div class="subject-name"><BookOpen :size="14" /> {{ r.subject }}</div>
            </td>
            <td>
              <div class="group-cell"><Users :size="14" color="#94a3b8" /> {{ r.groups }}</div>
            </td>
            <td>
              <span class="type-badge" :class="r.lessonType === 'Lecture' ? 'badge-lek' : 'badge-prak'">
                {{ lessonTypeShort[r.lessonType] }}
              </span>
            </td>
            <td class="ul-num">{{ r.plannedPairs }}</td>
            <td class="ul-num">{{ r.placedPairs }}</td>
            <td class="ul-num">
              <span :class="['ul-gap', r.placedPairs === 0 ? 'ul-none' : 'ul-partial']">
                {{ r.unplacedPairs }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.ul-container { background: white; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); display: flex; flex-direction: column; min-height: calc(100vh - 60px); font-family: sans-serif; }
.ul-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; flex-wrap: wrap; gap: 12px; }
.title-group { display: flex; align-items: center; gap: 12px; }
h2 { font-size: 20px; color: #1e293b; margin: 0; }
.ul-summary { display: flex; gap: 10px; }
.ul-chip { display: inline-flex; align-items: center; gap: 6px; background: #f8fafc; border: 1px solid #e2e8f0; color: #1e293b; padding: 8px 14px; border-radius: 8px; font-size: 13px; font-weight: 600; }
.ul-chip-amber { background: #fffbeb; border-color: #fde68a; color: #b45309; }

.filters-bar { display: flex; gap: 20px; padding: 0 24px 20px 24px; border-bottom: 1px solid #f1f5f9; flex-wrap: wrap; }
.filter-group { display: flex; flex-direction: column; gap: 8px; flex: 1; min-width: 180px; }
label { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; }
.select-dropdown { height: 44px; padding: 0 12px; border: 1px solid #cbd5e1; border-radius: 8px; background: white; font-size: 14px; outline: none; cursor: pointer; }
.select-dropdown:disabled { background: #f8fafc; cursor: not-allowed; color: #94a3b8; }

.table-wrapper { overflow-x: auto; padding: 0 24px 24px 24px; }
.state-msg { padding: 40px 0; text-align: center; color: #64748b; font-size: 14px; }
.state-error { color: #dc2626; }
.state-ok { color: #15803d; }

.ul-table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 16px; }
.ul-table thead th { color: #64748b; font-weight: 600; padding: 12px 8px; border-bottom: 2px solid #e2e8f0; text-align: center; font-size: 12px; }
.main-th { text-align: left !important; min-width: 260px; }
.ul-num { text-align: center; white-space: nowrap; }

.data-row td { padding: 14px 8px; border-bottom: 1px solid #f1f5f9; text-align: center; vertical-align: middle; }
.data-row .main-td { text-align: left; }
.data-row:hover td { background: #f8fafc; }
.teacher-name { font-weight: 600; color: #1e293b; font-size: 13px; margin-bottom: 4px; }
.subject-name { display: flex; align-items: center; gap: 6px; color: #94a3b8; font-size: 12px; }
.group-cell { display: flex; align-items: center; justify-content: center; gap: 6px; color: #475569; }
.type-badge { padding: 4px 10px; border-radius: 12px; font-size: 11px; font-weight: 700; }
.badge-lek { background: #eff6ff; color: #3b82f6; }
.badge-prak { background: #f0fdf4; color: #22c55e; }
.ul-gap { display: inline-block; min-width: 26px; padding: 3px 10px; border-radius: 9999px; font-weight: 700; font-size: 12px; }
.ul-partial { background: #fef3c7; color: #b45309; }
.ul-none { background: #fee2e2; color: #b91c1c; }
</style>
