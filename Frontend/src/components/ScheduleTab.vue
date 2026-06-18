<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  Calendar, RotateCcw, Sparkles, ChevronDown, ChevronUp, Download,
  User, Users, MapPin, Plus, CheckCircle2, GraduationCap, Building2,
} from 'lucide-vue-next'
import BaseButton from './BaseButton.vue'
import { lookups } from '../api/lookups'
import { lessons } from '../api/lessons'
import { ApiError } from '../api/http'
import type {
  InstituteDto, DepartmentDto, TeacherDto, DegreeDto, CourseDto, GroupDto, BuildingDto, RoomDto,
} from '../api/types'

type Entity = 'teachers' | 'groups' | 'rooms'
const currentEntity = ref<Entity>('teachers')
const selectedWeek = ref<number | string>('')
const totalWeeks = 18

const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб']
const timeSlots = [
  { id: 1, start: '08:00', end: '09:30' }, { id: 2, start: '09:40', end: '11:10' },
  { id: 3, start: '11:20', end: '12:50' }, { id: 4, start: '13:20', end: '14:50' },
  { id: 5, start: '15:00', end: '16:30' }, { id: 6, start: '16:40', end: '18:10' },
  { id: 7, start: '18:20', end: '19:50' }, { id: 8, start: '20:00', end: '21:30' },
]

// --- Справочники ---
const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])
const degrees = ref<DegreeDto[]>([])
const courses = ref<CourseDto[]>([])
const groups = ref<GroupDto[]>([])
const buildings = ref<BuildingDto[]>([])
const rooms = ref<RoomDto[]>([])

// Выбранные значения каскадов.
const selInstitute = ref('')
const selDept = ref('')
const selTeacher = ref('')
const selDegree = ref('')
const selCourse = ref('')
const selGroup = ref('')
const selBuilding = ref('')
const selRoom = ref('')

// Конечный выбранный объект (преподаватель / группа / аудитория).
const selectedLeafId = computed(() => {
  if (currentEntity.value === 'teachers') return selTeacher.value
  if (currentEntity.value === 'groups') return selGroup.value
  return selRoom.value
})
const hasSelection = computed(() => selectedLeafId.value !== '')

// --- Занятия выбранного объекта ---
const lessonCount = ref(0)
const lessonsLoading = ref(false)
const banner = ref('')

const isAutoGenerateOpen = ref(false)

async function loadLessons() {
  banner.value = ''
  if (!hasSelection.value) { lessonCount.value = 0; return }
  lessonsLoading.value = true
  try {
    const entity = currentEntity.value === 'teachers' ? 'teacher'
      : currentEntity.value === 'groups' ? 'group' : 'room'
    // weekId на бэке — это GUID; UI оперирует порядковым номером недели,
    // сопоставления пока нет (нет эндпоинта недель), поэтому грузим за семестр.
    const data = await lessons.byEntity(entity, selectedLeafId.value)
    lessonCount.value = data.length
  } catch (e) {
    banner.value = e instanceof ApiError ? e.message : 'Не удалось загрузить занятия.'
    lessonCount.value = 0
  } finally {
    lessonsLoading.value = false
  }
}

// Каскады справочников.
watch(currentEntity, () => { banner.value = '' })

watch(selInstitute, async (id) => {
  selDept.value = ''; selTeacher.value = ''; selDegree.value = ''; selCourse.value = ''; selGroup.value = ''
  departments.value = []; teachers.value = []; degrees.value = []; courses.value = []; groups.value = []
  if (!id) return
  if (currentEntity.value === 'teachers') {
    [departments.value, teachers.value] = await Promise.all([
      lookups.departments(id), lookups.teachers({ instituteId: id }),
    ])
  } else if (currentEntity.value === 'groups') {
    degrees.value = await lookups.degrees(id)
  }
})

watch(selDept, async (id) => {
  if (currentEntity.value !== 'teachers') return
  selTeacher.value = ''
  teachers.value = await lookups.teachers({ instituteId: selInstitute.value, departmentId: id || undefined })
})

watch(selDegree, async (id) => {
  selCourse.value = ''; selGroup.value = ''; courses.value = []; groups.value = []
  if (id) courses.value = await lookups.courses(id, selInstitute.value)
})
watch(selCourse, async (id) => {
  selGroup.value = ''; groups.value = []
  if (id) groups.value = await lookups.groups({ courseId: id })
})

watch(selBuilding, async (id) => {
  selRoom.value = ''; rooms.value = []
  if (id) rooms.value = await lookups.rooms(id)
})

watch(selectedLeafId, loadLessons)

async function generate(scope: 'university' | 'teacher' | 'institute') {
  isAutoGenerateOpen.value = false
  if (scope === 'institute') {
    // Генерация требует semesterId, а эндпоинта списка семестров пока нет — см. README.
    banner.value = 'Генерация по институту требует выбора семестра (эндпоинт семестров ещё не реализован на бэкенде).'
    return
  }
  banner.value = scope === 'university'
    ? 'Генерация по всему вузу пока не поддержана бэкендом.'
    : 'Генерация по преподавателю пока не поддержана бэкендом (есть только по институту).'
}

onMounted(async () => {
  institutes.value = await lookups.institutes().catch(() => [])
  buildings.value = await lookups.buildings().catch(() => [])
})
</script>

<template>
  <div class="schedule-container">

    <!-- 1. ВЕРХНЯЯ ПАНЕЛЬ -->
    <header class="top-bar">
      <div class="left-controls">
        <Calendar :size="18" color="#64748b" />
        <span class="label">Неделя:</span>
        <select class="select-dropdown week-select" :class="{ 'is-placeholder': selectedWeek === '' }" v-model="selectedWeek">
          <option value="" disabled hidden>Выберите неделю</option>
          <option v-for="week in totalWeeks" :key="week" :value="week">{{ week }} неделя</option>
        </select>

        <div class="badge badge-green"><CheckCircle2 :size="14" /> Текущая</div>
      </div>

      <div class="right-controls">
        <BaseButton variant="outline"><RotateCcw :size="16" /> Сбросить до выгруженного</BaseButton>
        <div class="dropdown-wrapper">
          <BaseButton variant="gradient" @click="isAutoGenerateOpen = !isAutoGenerateOpen">
            <Sparkles :size="16" /> Автогенерация
            <ChevronUp v-if="isAutoGenerateOpen" :size="16" />
            <ChevronDown v-else :size="16" />
          </BaseButton>

          <div v-if="isAutoGenerateOpen" class="dropdown-menu">
            <div class="dropdown-item" @click="generate('university')">
              <GraduationCap :size="18" class="dd-icon" /><span>Для всего университета</span>
            </div>
            <div class="dropdown-item" @click="generate('teacher')">
              <User :size="18" class="dd-icon" /><span>Для выбранного преподавателя</span>
            </div>
            <div class="dropdown-item" @click="generate('institute')">
              <Building2 :size="18" class="dd-icon" /><span>Для выбранного института</span>
            </div>
          </div>
        </div>
        <BaseButton variant="outline"><Download :size="16" /> Выгрузить</BaseButton>
      </div>
    </header>

    <!-- 2. ПАНЕЛЬ ВЫБОРА СУЩНОСТИ -->
    <div class="entity-tabs">
      <button class="entity-btn" :class="{ active: currentEntity === 'teachers' }" @click="currentEntity = 'teachers'">
        <User :size="16" :color="currentEntity === 'teachers' ? 'white' : '#64748b'" /> Преподаватели
      </button>
      <button class="entity-btn" :class="{ active: currentEntity === 'groups' }" @click="currentEntity = 'groups'">
        <Users :size="16" :color="currentEntity === 'groups' ? 'white' : '#64748b'" /> Группы
      </button>
      <button class="entity-btn" :class="{ active: currentEntity === 'rooms' }" @click="currentEntity = 'rooms'">
        <MapPin :size="16" :color="currentEntity === 'rooms' ? 'white' : '#64748b'" /> Аудитории
      </button>
    </div>

    <!-- 3. ПАНЕЛЬ ФИЛЬТРОВ -->
    <div class="filters-bar">
      <div class="filters-left">
        <template v-if="currentEntity === 'teachers'">
          <select class="select-dropdown" v-model="selInstitute">
            <option value="">Все институты</option>
            <option v-for="i in institutes" :key="i.id" :value="i.id">{{ i.name }}</option>
          </select>
          <select class="select-dropdown" v-model="selDept" :disabled="!selInstitute">
            <option value="">Все кафедры</option>
            <option v-for="d in departments" :key="d.id" :value="d.id">{{ d.name }}</option>
          </select>
          <select class="select-dropdown" v-model="selTeacher" :disabled="!selInstitute">
            <option value="">Выберите преподавателя</option>
            <option v-for="t in teachers" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
        </template>

        <template v-if="currentEntity === 'groups'">
          <select class="select-dropdown" v-model="selInstitute">
            <option value="">Все институты</option>
            <option v-for="i in institutes" :key="i.id" :value="i.id">{{ i.name }}</option>
          </select>
          <select class="select-dropdown" v-model="selDegree" :disabled="!selInstitute">
            <option value="">Все уровни</option>
            <option v-for="d in degrees" :key="d.id" :value="d.id">{{ d.typeDegree }}</option>
          </select>
          <select class="select-dropdown" v-model="selCourse" :disabled="!selDegree">
            <option value="">Все курсы</option>
            <option v-for="c in courses" :key="c.id" :value="c.id">{{ c.number }} курс</option>
          </select>
          <select class="select-dropdown" v-model="selGroup" :disabled="!selCourse">
            <option value="">Выберите группу</option>
            <option v-for="g in groups" :key="g.id" :value="g.id">{{ g.name }}</option>
          </select>
        </template>

        <template v-if="currentEntity === 'rooms'">
          <select class="select-dropdown" v-model="selBuilding">
            <option value="">Все корпусы</option>
            <option v-for="b in buildings" :key="b.id" :value="b.id">{{ b.name }}</option>
          </select>
          <select class="select-dropdown" v-model="selRoom" :disabled="!selBuilding">
            <option value="">Выберите аудиторию...</option>
            <option v-for="r in rooms" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select>
        </template>
      </div>

      <BaseButton variant="primary"><Plus :size="16" /> Добавить пару</BaseButton>
    </div>

    <!-- 4. СЕТКА ИЛИ ПУСТОЕ СОСТОЯНИЕ -->
    <div v-if="hasSelection" class="schedule-grid-container">

      <div class="alert-banner" :class="{ 'is-error': banner }">
        <span v-if="banner">{{ banner }}</span>
        <span v-else-if="lessonsLoading">Загрузка занятий…</span>
        <span v-else>Найдено занятий: {{ lessonCount }}. Раскладка по сетке требует метаданных слотов (день/номер пары) — см. README.</span>
      </div>

      <div class="schedule-table">
        <div class="grid-row header-row">
          <div class="time-col-header">Пара</div>
          <div v-for="day in daysOfWeek" :key="day" class="day-header">{{ day }}</div>
        </div>

        <div v-for="slot in timeSlots" :key="slot.id" class="grid-row">
          <div class="time-cell">
            <span class="slot-number">{{ slot.id }}</span>
            <span class="slot-time">{{ slot.start }}<br>—<br>{{ slot.end }}</span>
          </div>
          <div v-for="day in daysOfWeek" :key="day" class="empty-cell"></div>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <div class="empty-icon-wrapper">
        <User v-if="currentEntity === 'teachers'" :size="40" color="#94a3b8" />
        <Users v-else-if="currentEntity === 'groups'" :size="40" color="#94a3b8" />
        <MapPin v-else :size="40" color="#94a3b8" />
      </div>
      <h3>
        <span v-if="currentEntity === 'teachers'">Выберите преподавателя</span>
        <span v-else-if="currentEntity === 'groups'">Выберите группу</span>
        <span v-else>Выберите аудиторию</span>
      </h3>
      <p>Используйте фильтры выше для поиска и<br>выбора объекта расписания.</p>
    </div>

  </div>
</template>

<style scoped>
.schedule-container { background-color: white; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); display: flex; flex-direction: column; min-height: calc(100vh - 60px); font-family: sans-serif; }
.top-bar { display: flex; justify-content: space-between; align-items: center; padding: 16px 24px; border-bottom: 1px solid #f1f5f9; }
.left-controls, .right-controls { display: flex; align-items: center; gap: 16px; }
.label { font-size: 14px; color: #64748b; }

.badge { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 500; }
.badge-green { color: #059669; background-color: transparent; border: none; padding: 6px 0; }

.entity-tabs { display: flex; gap: 12px; padding: 20px 24px 0 24px; }
.entity-btn { display: flex; align-items: center; gap: 8px; padding: 10px 24px; border-radius: 12px; border: 1px solid #cbd5e1; background: white; color: #334155; font-size: 14px; font-weight: 500; cursor: pointer; transition: all 0.2s; }
.entity-btn:hover:not(.active) { background-color: #f8fafc; }
.entity-btn.active { background-color: #1a4d9c; color: white; border-color: #1a4d9c; }

.filters-bar { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; }
.filters-left { display: flex; gap: 12px; }
.select-dropdown { padding: 10px 36px 10px 16px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: white; color: #334155; font-size: 14px; outline: none; appearance: none; background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%2394a3b8' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E"); background-repeat: no-repeat; background-position: right 12px center; min-width: 180px; cursor: pointer; }
.select-dropdown:hover:not(:disabled) { border-color: #94a3b8; }
.week-select { min-width: 130px; font-weight: 500; color: #0f172a; }
.select-dropdown.is-placeholder { color: #94a3b8; }
.select-dropdown option { color: #334155; }
.select-dropdown:disabled { background-color: #f8fafc; color: #94a3b8; border-color: #e2e8f0; cursor: not-allowed; opacity: 0.7; }

.empty-state { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; }
.empty-icon-wrapper { background-color: #f1f5f9; padding: 24px; border-radius: 16px; margin-bottom: 24px; }
.empty-state h3 { margin: 0 0 8px 0; color: #334155; font-size: 18px; }
.empty-state p { margin: 0; color: #94a3b8; font-size: 14px; line-height: 1.5; }

.schedule-grid-container { padding: 0 24px 24px 24px; flex-grow: 1; display: flex; flex-direction: column; }

.alert-banner { display: flex; align-items: center; gap: 8px; background-color: #eff6ff; color: #1d4ed8; padding: 12px 16px; border-radius: 8px; font-size: 14px; font-weight: 500; margin-bottom: 20px; }
.alert-banner.is-error { background-color: #fef2f2; color: #dc2626; }

.schedule-table { display: flex; flex-direction: column; gap: 12px; }
.grid-row { display: grid; grid-template-columns: 60px repeat(6, 1fr); gap: 12px; }
.time-col-header { font-size: 13px; color: #94a3b8; display: flex; align-items: center; justify-content: center; }
.day-header { background-color: #f1f5f9; color: #1a4d9c; font-weight: 600; font-size: 14px; padding: 12px; border-radius: 8px; text-align: center; }
.time-cell { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 12px 4px; min-height: 80px; }
.slot-number { color: #1a4d9c; font-weight: 700; font-size: 16px; margin-bottom: 4px; }
.slot-time { color: #94a3b8; font-size: 11px; line-height: 1.2; text-align: center; }
.empty-cell { background-color: white; border: 1px solid #e2e8f0; border-radius: 8px; transition: background-color 0.2s; }
.empty-cell:hover { background-color: #f8fafc; cursor: pointer; }

.dropdown-wrapper { position: relative; }
.dropdown-menu { position: absolute; top: calc(100% + 8px); right: 0; background: white; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05); padding: 8px; min-width: 270px; z-index: 50; display: flex; flex-direction: column; gap: 4px; }
.dropdown-item { display: flex; align-items: center; gap: 12px; padding: 10px 14px; border-radius: 8px; cursor: pointer; color: #1e293b; font-size: 14px; font-weight: 500; transition: all 0.2s ease; }
.dropdown-item .dd-icon { color: #475569; transition: color 0.2s ease; }
.dropdown-item:hover { background-color: #f4f6f8; color: #1a4d9c; }
.dropdown-item:hover .dd-icon { color: #1a4d9c; }
</style>
