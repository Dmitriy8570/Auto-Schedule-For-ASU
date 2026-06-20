<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  Settings2, Search, User, MapPin, Users, BookOpen, Database,
  ChevronDown, GraduationCap, Building2, Layers, Trash2, Plus, Wrench, Save,
  SlidersHorizontal, Clock, Copy, X,
} from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'
import TimeGrid from './TimeGrid.vue'
import { lookups } from '../api/lookups'
import { management } from '../api/management'
import { workloads } from '../api/workloads'
import { constraints } from '../api/constraints'
import { ApiError } from '../api/http'
import type {
  RoomDto, EquipmentDto, ConstraintConfigDto, BuildingDto, ConstraintType,
  InstituteDto, DepartmentDto, TeacherDto, DegreeDto, CourseDto, GroupDto,
  WorkloadItemDto, AvailabilityState, AvailabilityCellDto, CurriculumConstraintsDto,
  Shift, TypeDegree, LessonType,
} from '../api/types'

type Tab = 'teachers' | 'rooms' | 'groups' | 'load' | 'objects'

const activeTab = ref<Tab>('teachers')
const searchQuery = ref('')

// ============ Справочники (реальные данные) ============
const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])
const buildings = ref<BuildingDto[]>([])
const rooms = ref<RoomDto[]>([])
const degrees = ref<DegreeDto[]>([])
const courses = ref<CourseDto[]>([])
const groups = ref<GroupDto[]>([])
const workloadItems = ref<WorkloadItemDto[]>([])
const equipments = ref<EquipmentDto[]>([])

const loaded = ref<Record<Tab, boolean>>(
  { teachers: false, rooms: false, groups: false, load: false, objects: false })
const treeError = ref('')

// Свёрнутые узлы дерева (по умолчанию всё развёрнуто).
const collapsed = ref<Set<string>>(new Set())
const isOpen = (id: string) => !collapsed.value.has(id)
function toggle(id: string) {
  const s = new Set(collapsed.value)
  if (s.has(id)) s.delete(id); else s.add(id)
  collapsed.value = s
}

const degreeLabels: Record<TypeDegree, string> = {
  Secondary: 'СПО', Bachelor: 'Бакалавриат', Specialist: 'Специалитет',
  Master: 'Магистратура', Postgraduate: 'Аспирантура', Doctoral: 'Докторантура',
}
const lessonTypeLabels: Record<LessonType, string> = {
  Lecture: 'Лекция', Seminar: 'Семинар', Laboratory: 'Лаб. работа',
  Consultation: 'Консультация', Examination: 'Экзамен',
}
const shiftLabels: Record<Shift, string> = {
  First: '1-я смена', Second: '2-я смена', Evening: 'Вечерняя', Unspecified: 'Не указана',
}

// ============ Загрузка данных вкладок (лениво) ============
async function ensureInstitutes() {
  if (!institutes.value.length) institutes.value = await lookups.institutes()
}
async function ensureBuildings() {
  if (!buildings.value.length) buildings.value = await lookups.buildings()
}

async function ensureLoaded(tab: Tab) {
  if (loaded.value[tab]) return
  treeError.value = ''
  try {
    if (tab === 'teachers') {
      await ensureInstitutes()
      ;[departments.value, teachers.value] = await Promise.all([lookups.departments(), lookups.teachers()])
    } else if (tab === 'rooms') {
      await ensureBuildings()
      rooms.value = await lookups.rooms()
    } else if (tab === 'groups') {
      await ensureInstitutes()
      ;[degrees.value, courses.value, groups.value] =
        await Promise.all([lookups.degrees(), lookups.courses(), lookups.groups()])
    } else if (tab === 'load') {
      await ensureBuildings()
      const [wl, eq] = await Promise.all([workloads.list({ pageSize: 500 }), management.equipments()])
      workloadItems.value = wl.items
      equipments.value = eq
    } else if (tab === 'objects') {
      await loadObjects()
    }
    loaded.value = { ...loaded.value, [tab]: true }
  } catch (e) {
    treeError.value = e instanceof ApiError ? e.message : 'Не удалось загрузить данные.'
  }
}

// ============ Деревья (с фильтром по поиску) ============
const q = computed(() => searchQuery.value.trim().toLowerCase())
const match = (s: string) => !q.value || s.toLowerCase().includes(q.value)

const teacherTree = computed(() =>
  institutes.value.map(inst => ({
    ...inst,
    depts: departments.value
      .filter(d => d.instituteId === inst.id)
      .map(dept => ({ ...dept, teachers: teachers.value.filter(t => t.departmentId === dept.id && match(t.name)) }))
      .filter(dept => !q.value || dept.teachers.length),
  })).filter(inst => !q.value || inst.depts.length))

const roomTree = computed(() =>
  buildings.value.map(b => ({
    ...b,
    rooms: rooms.value.filter(r => r.buildingId === b.id && match(r.name)),
  })).filter(b => !q.value || b.rooms.length))

const groupTree = computed(() =>
  institutes.value.map(inst => ({
    ...inst,
    degrees: degrees.value
      .filter(d => d.instituteId === inst.id)
      .map(deg => ({
        ...deg,
        courses: courses.value
          .filter(c => c.degreeId === deg.id)
          .map(crs => ({ ...crs, groups: groups.value.filter(g => g.courseId === crs.id && match(g.name)) }))
          .filter(crs => !q.value || crs.groups.length),
      }))
      .filter(deg => !q.value || deg.courses.length),
  })).filter(inst => !q.value || inst.degrees.length))

const workloadList = computed(() =>
  workloadItems.value.filter(w => !q.value || match(w.subject) || match(w.teacher) || match(w.group)))

const tabCount = computed(() => ({
  teachers: teachers.value.length, rooms: rooms.value.length,
  groups: groups.value.length, load: workloadItems.value.length,
}))

const lessonTypeClass = (t: LessonType) =>
  ({ Lecture: 'badge-lecture', Seminar: 'badge-practice', Laboratory: 'badge-lab' } as Record<string, string>)[t] ?? ''

// ============ Выбор объекта и правые панели ============
type SelKind = 'teacher' | 'room' | 'group' | 'curriculum'
interface Selection { kind: SelKind; id: string; name: string; sublabel: string }
const selected = ref<Selection | null>(null)

const panelStatus = ref<'' | 'saving' | 'saved'>('')
const panelError = ref('')

const availabilityGrid = ref<Record<string, AvailabilityState>>({})
const groupShift = ref<Shift>('Unspecified')
const curConstraints = ref<CurriculumConstraintsDto>(
  { requiredEquipmentIds: [], isParallel: false, isDouble: false, preferredBuildingId: null })
const equipmentToAdd = ref('')

function resetPanel() { panelStatus.value = ''; panelError.value = '' }

async function selectTeacher(t: TeacherDto) {
  selected.value = { kind: 'teacher', id: t.id, name: t.name, sublabel: t.departmentName }
  resetPanel()
  await loadAvailability('teacher', t.id)
}
async function selectRoom(r: RoomDto) {
  selected.value = { kind: 'room', id: r.id, name: r.name, sublabel: `${r.buildingName} · ${r.capacity} мест` }
  resetPanel()
  await loadAvailability('classroom', r.id)
}
function selectGroup(g: GroupDto) {
  selected.value = { kind: 'group', id: g.id, name: g.name, sublabel: `${g.courseNumber} курс · ${g.studentCount} чел.` }
  resetPanel()
  groupShift.value = g.shift
}
async function selectCurriculum(w: WorkloadItemDto) {
  selected.value = { kind: 'curriculum', id: w.curriculumId, name: w.subject, sublabel: `${w.teacher} · ${w.group}` }
  resetPanel()
  await loadCurriculumConstraints(w.curriculumId)
}

function gridFromCells(cells: AvailabilityCellDto[]): Record<string, AvailabilityState> {
  const g: Record<string, AvailabilityState> = {}
  for (const c of cells) g[`${c.dayOfWeek}-${c.pairNumber}`] = c.state
  return g
}
function cellsFromGrid(grid: Record<string, AvailabilityState>): AvailabilityCellDto[] {
  const cells: AvailabilityCellDto[] = []
  for (const [key, state] of Object.entries(grid)) {
    if (state === 'Neutral') continue
    const parts = key.split('-')
    cells.push({ dayOfWeek: Number(parts[0]), pairNumber: Number(parts[1]), state })
  }
  return cells
}

async function loadAvailability(kind: 'teacher' | 'classroom', id: string) {
  panelError.value = ''
  try {
    const cells = kind === 'teacher'
      ? await constraints.teacherAvailability(id)
      : await constraints.classroomAvailability(id)
    availabilityGrid.value = gridFromCells(cells)
  } catch (e) {
    availabilityGrid.value = {}
    panelError.value = e instanceof ApiError ? e.message : 'Не удалось загрузить доступность.'
  }
}

async function loadCurriculumConstraints(id: string) {
  panelError.value = ''
  try {
    const c = await constraints.curriculumConstraints(id)
    curConstraints.value = {
      requiredEquipmentIds: [...c.requiredEquipmentIds],
      isParallel: c.isParallel, isDouble: c.isDouble, preferredBuildingId: c.preferredBuildingId,
    }
  } catch (e) {
    curConstraints.value = { requiredEquipmentIds: [], isParallel: false, isDouble: false, preferredBuildingId: null }
    panelError.value = e instanceof ApiError ? e.message : 'Не удалось загрузить ограничения.'
  }
}

const availableEquipment = computed(() =>
  equipments.value.filter(e => !curConstraints.value.requiredEquipmentIds.includes(e.id)))
const equipmentName = (id: string) => equipments.value.find(e => e.id === id)?.name ?? id
function addEquipment() {
  const id = equipmentToAdd.value
  if (id && !curConstraints.value.requiredEquipmentIds.includes(id))
    curConstraints.value.requiredEquipmentIds.push(id)
  equipmentToAdd.value = ''
}
function removeEquipment(id: string) {
  curConstraints.value.requiredEquipmentIds =
    curConstraints.value.requiredEquipmentIds.filter(x => x !== id)
}

async function savePanel() {
  if (!selected.value) return
  const sel = selected.value
  panelStatus.value = 'saving'; panelError.value = ''
  try {
    if (sel.kind === 'teacher') await constraints.setTeacherAvailability(sel.id, cellsFromGrid(availabilityGrid.value))
    else if (sel.kind === 'room') await constraints.setClassroomAvailability(sel.id, cellsFromGrid(availabilityGrid.value))
    else if (sel.kind === 'group') await constraints.setGroupShift(sel.id, groupShift.value)
    else if (sel.kind === 'curriculum') await constraints.setCurriculumConstraints(sel.id, { ...curConstraints.value })
    panelStatus.value = 'saved'
    setTimeout(() => { if (panelStatus.value === 'saved') panelStatus.value = '' }, 2500)
  } catch (e) {
    panelStatus.value = ''
    panelError.value = e instanceof ApiError ? e.message : 'Не удалось сохранить.'
  }
}

// ============ Объекты системы (CRUD — без изменений по сути) ============
const activeObjectTab = ref<'audiences' | 'equipment' | 'weights'>('audiences')
const isCreatingRoom = ref(false)
const isCreatingEquipment = ref(false)
const objectsError = ref('')

const classrooms = ref<RoomDto[]>([])
const constraintWeights = ref<ConstraintConfigDto[]>([])

const roomForm = ref<{ id: string | null; name: string; buildingId: string; capacity: number }>(
  { id: null, name: '', buildingId: '', capacity: 30 })
const newEquipmentName = ref('')

const constraintLabels: Record<ConstraintType, string> = {
  TeacherGap: 'Окна в расписании преподавателя',
  StudentGap: 'Окна в расписании группы',
  ClassroomAvailability: 'Недоступная аудитория',
  TeacherAvailability: 'Недоступный слот преподавателя',
}

async function loadObjects() {
  objectsError.value = ''
  ;[classrooms.value, equipments.value, buildings.value, constraintWeights.value] = await Promise.all([
    management.classrooms(), management.equipments(), lookups.buildings(), management.constraints(),
  ])
}

function openRoomForm() {
  roomForm.value = { id: null, name: '', buildingId: buildings.value[0]?.id ?? '', capacity: 30 }
  isCreatingRoom.value = true
}
function editRoom(r: RoomDto) {
  roomForm.value = { id: r.id, name: r.name, buildingId: r.buildingId, capacity: r.capacity }
  isCreatingRoom.value = true
}
async function saveRoom() {
  const f = roomForm.value
  if (!f.name.trim() || !f.buildingId) { objectsError.value = 'Укажите название и корпус.'; return }
  objectsError.value = ''
  try {
    const body = { name: f.name.trim(), capacity: Number(f.capacity), buildingId: f.buildingId }
    if (f.id) await management.updateClassroom(f.id, body)
    else await management.createClassroom(body)
    isCreatingRoom.value = false
    classrooms.value = await management.classrooms()
  } catch (e) {
    objectsError.value = e instanceof ApiError ? e.message : 'Не удалось сохранить аудиторию.'
  }
}
async function deleteRoom(id: string) {
  if (!confirm('Удалить аудиторию?')) return
  try { await management.deleteClassroom(id); classrooms.value = await management.classrooms() }
  catch (e) { objectsError.value = e instanceof ApiError ? e.message : 'Не удалось удалить аудиторию.' }
}

async function saveEquipment() {
  const name = newEquipmentName.value.trim()
  if (!name) { isCreatingEquipment.value = false; return }
  objectsError.value = ''
  try {
    await management.createEquipment(name)
    newEquipmentName.value = ''
    isCreatingEquipment.value = false
    equipments.value = await management.equipments()
  } catch (e) {
    objectsError.value = e instanceof ApiError ? e.message : 'Не удалось добавить оборудование.'
  }
}
async function deleteEquipment(id: string) {
  if (!confirm('Удалить тип оборудования?')) return
  try { await management.deleteEquipment(id); equipments.value = await management.equipments() }
  catch (e) { objectsError.value = e instanceof ApiError ? e.message : 'Не удалось удалить оборудование.' }
}

async function saveConstraint(c: ConstraintConfigDto) {
  objectsError.value = ''
  try { await management.updateConstraint(c.id, Number(c.penalty)) }
  catch (e) { objectsError.value = e instanceof ApiError ? e.message : 'Не удалось сохранить вес.' }
}

// ============ Жизненный цикл ============
watch(activeTab, (t) => { selected.value = null; resetPanel(); ensureLoaded(t) })
onMounted(() => ensureLoaded(activeTab.value))
</script>

<template>
  <div class="settings-container">

    <aside class="sidebar-panel">
      <header class="panel-header">
        <Settings2 :size="20" color="#1e293b" />
        <h2>Ограничения</h2>
      </header>

      <div class="search-box">
        <BaseInput v-model="searchQuery" placeholder="Поиск...">
          <template #left-icon><Search :size="16" /></template>
        </BaseInput>
      </div>

      <div class="object-tabs">
        <div class="obj-tab" :class="{ active: activeTab === 'teachers' }" @click="activeTab = 'teachers'"><User :size="20" /><span>Препод.</span></div>
        <div class="obj-tab" :class="{ active: activeTab === 'rooms' }" @click="activeTab = 'rooms'"><MapPin :size="20" /><span>Ауд.</span></div>
        <div class="obj-tab" :class="{ active: activeTab === 'groups' }" @click="activeTab = 'groups'"><Users :size="20" /><span>Группы</span></div>
        <div class="obj-tab" :class="{ active: activeTab === 'load' }" @click="activeTab = 'load'"><BookOpen :size="20" /><span>Нагрузка</span></div>
        <div class="obj-tab" :class="{ active: activeTab === 'objects' }" @click="activeTab = 'objects'"><Database :size="20" /><span>Объекты</span></div>
      </div>

      <div v-if="treeError" class="objects-error">{{ treeError }}</div>

      <template v-if="activeTab !== 'objects'">
        <div class="list-header">
          <span class="list-title">{{ activeTab === 'teachers' ? 'Преподаватели' : activeTab === 'rooms' ? 'Аудитории' : activeTab === 'groups' ? 'Группы' : 'Нагрузка' }}</span>
          <span class="badge">{{ tabCount[activeTab as 'teachers'|'rooms'|'groups'|'load'] }}</span>
        </div>
      </template>

      <!-- Преподаватели: Институт → Кафедра → Преподаватель -->
      <div v-if="activeTab === 'teachers'" class="tree-view">
        <div v-for="inst in teacherTree" :key="inst.id" class="tree-node">
          <div class="node-header" @click="toggle('i-' + inst.id)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('i-' + inst.id) }" />
            <GraduationCap :size="16" color="#10b981" /><span class="node-text">{{ inst.name }}</span>
          </div>
          <div v-show="isOpen('i-' + inst.id)" class="node-children">
            <div v-for="dept in inst.depts" :key="dept.id" class="tree-node">
              <div class="node-header" @click="toggle('d-' + dept.id)">
                <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('d-' + dept.id) }" />
                <Building2 :size="16" color="#64748b" /><span class="node-text">{{ dept.name }}</span>
              </div>
              <div v-show="isOpen('d-' + dept.id)" class="node-children">
                <div v-for="t in dept.teachers" :key="t.id" class="leaf-node"
                     :class="{ selected: selected?.kind === 'teacher' && selected.id === t.id }" @click="selectTeacher(t)">
                  <div class="leaf-title">{{ t.name }}</div>
                  <div class="leaf-subtitle">{{ dept.name }}</div>
                </div>
                <div v-if="!dept.teachers.length" class="muted-note small">нет преподавателей</div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="loaded.teachers && !teacherTree.length" class="muted-note">Ничего не найдено.</div>
      </div>

      <!-- Аудитории: Корпус → Аудитория -->
      <div v-else-if="activeTab === 'rooms'" class="tree-view">
        <div v-for="b in roomTree" :key="b.id" class="tree-node">
          <div class="node-header" @click="toggle('b-' + b.id)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('b-' + b.id) }" />
            <Building2 :size="16" color="#a855f7" /><span class="node-text">{{ b.name }}</span>
          </div>
          <div v-show="isOpen('b-' + b.id)" class="node-children">
            <div v-for="r in b.rooms" :key="r.id" class="leaf-node"
                 :class="{ selected: selected?.kind === 'room' && selected.id === r.id }" @click="selectRoom(r)">
              <div class="leaf-title">{{ r.name }}</div>
              <div class="leaf-subtitle">{{ b.name }} · {{ r.capacity }} мест</div>
            </div>
            <div v-if="!b.rooms.length" class="muted-note small">нет аудиторий</div>
          </div>
        </div>
        <div v-if="loaded.rooms && !roomTree.length" class="muted-note">Ничего не найдено.</div>
      </div>

      <!-- Группы: Институт → Ступень → Курс → Группа -->
      <div v-else-if="activeTab === 'groups'" class="tree-view">
        <div v-for="inst in groupTree" :key="inst.id" class="tree-node">
          <div class="node-header" @click="toggle('gi-' + inst.id)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('gi-' + inst.id) }" />
            <GraduationCap :size="16" color="#10b981" /><span class="node-text">{{ inst.name }}</span>
          </div>
          <div v-show="isOpen('gi-' + inst.id)" class="node-children">
            <div v-for="deg in inst.degrees" :key="deg.id" class="tree-node">
              <div class="node-header" @click="toggle('deg-' + deg.id)">
                <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('deg-' + deg.id) }" />
                <Layers :size="16" color="#14b8a6" /><span class="node-text">{{ degreeLabels[deg.typeDegree] }}</span>
              </div>
              <div v-show="isOpen('deg-' + deg.id)" class="node-children">
                <div v-for="crs in deg.courses" :key="crs.id" class="tree-node">
                  <div class="node-header" @click="toggle('crs-' + crs.id)">
                    <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !isOpen('crs-' + crs.id) }" />
                    <span class="node-text">{{ crs.number }} курс</span>
                  </div>
                  <div v-show="isOpen('crs-' + crs.id)" class="node-children">
                    <div v-for="g in crs.groups" :key="g.id" class="leaf-node"
                         :class="{ selected: selected?.kind === 'group' && selected.id === g.id }" @click="selectGroup(g)">
                      <div class="leaf-title">{{ g.name }}</div>
                      <div class="leaf-subtitle">{{ g.courseNumber }} курс · {{ g.studentCount }} чел.</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="loaded.groups && !groupTree.length" class="muted-note">Ничего не найдено.</div>
      </div>

      <!-- Нагрузка: список учебных планов -->
      <div v-else-if="activeTab === 'load'" class="load-card-list">
        <div v-for="w in workloadList" :key="w.curriculumId" class="load-card"
             :class="{ selected: selected?.kind === 'curriculum' && selected.id === w.curriculumId }" @click="selectCurriculum(w)">
          <div class="load-title">{{ w.subject }}</div>
          <div class="load-subtitle">{{ w.teacher }}</div>
          <div class="load-meta">
            <span class="type-badge" :class="lessonTypeClass(w.lessonType)">{{ lessonTypeLabels[w.lessonType] }}</span>
            <span class="load-group">{{ w.group }} · {{ w.semesterHours }}ч</span>
          </div>
        </div>
        <div v-if="loaded.load && !workloadList.length" class="muted-note">Ничего не найдено.</div>
      </div>

      <!-- Объекты: сводка -->
      <div v-if="activeTab === 'objects'" class="objects-summary">
        <Database :size="48" color="#cbd5e1" class="summary-icon" />
        <p class="summary-title">Управление объектами системы</p>
        <div class="summary-row"><span>Аудиторий:</span> <b>{{ classrooms.length }}</b></div>
        <div class="summary-row"><span>Типов оборудования:</span> <b>{{ equipments.length }}</b></div>
        <div class="summary-row"><span>Весов ограничений:</span> <b>{{ constraintWeights.length }}</b></div>
      </div>
    </aside>

    <main class="main-workspace" :class="{ 'is-objects-mode': activeTab === 'objects' }">

      <!-- ===== Объекты системы ===== -->
      <template v-if="activeTab === 'objects'">
        <div class="objects-sub-sidebar">
          <header class="workspace-tabs">
            <div class="wk-tab" :class="{ active: activeObjectTab === 'audiences' }" @click="activeObjectTab = 'audiences'; isCreatingRoom = false">
              <MapPin :size="16" /> Аудитории
            </div>
            <div class="wk-tab" :class="{ active: activeObjectTab === 'equipment' }" @click="activeObjectTab = 'equipment'; isCreatingEquipment = false">
              <Wrench :size="16" /> Оборудование
            </div>
            <div class="wk-tab" :class="{ active: activeObjectTab === 'weights' }" @click="activeObjectTab = 'weights'">
              <SlidersHorizontal :size="16" /> Веса
            </div>
          </header>

          <div class="sub-sidebar-content">
            <div v-if="objectsError" class="objects-error">{{ objectsError }}</div>

            <template v-if="activeObjectTab === 'audiences'">
              <button class="create-btn" @click="openRoomForm"><Plus :size="16" /> Создать аудиторию</button>
              <div v-if="classrooms.length === 0" class="muted-note">Аудиторий пока нет.</div>
              <div class="management-list">
                <div v-for="obj in classrooms" :key="obj.id" class="management-item" @click="editRoom(obj)">
                  <div class="m-info">
                    <div class="m-title">{{ obj.name }}</div>
                    <div class="m-desc">{{ obj.buildingName }} — {{ obj.capacity }} мест</div>
                  </div>
                  <button class="delete-btn" @click.stop="deleteRoom(obj.id)"><Trash2 :size="16" /></button>
                </div>
              </div>
            </template>

            <template v-if="activeObjectTab === 'equipment'">
              <div class="add-equipment-form">
                <input type="text" v-model="newEquipmentName" @keyup.enter="saveEquipment"
                       placeholder="Название оборудования..." class="eq-input" />
                <button class="add-eq-btn" @click="saveEquipment"><Plus :size="16" /></button>
              </div>
              <div v-if="equipments.length === 0" class="muted-note">Оборудование не добавлено.</div>
              <div class="equipment-list">
                <div v-for="item in equipments" :key="item.id" class="equipment-item">
                  <Wrench :size="14" color="#94a3b8" /><span class="eq-name">{{ item.name }}</span>
                  <button class="delete-btn eq-del" @click="deleteEquipment(item.id)"><Trash2 :size="14" /></button>
                </div>
              </div>
            </template>

            <template v-if="activeObjectTab === 'weights'">
              <div class="section-label">ВЕСА МЯГКИХ ОГРАНИЧЕНИЙ</div>
              <div v-if="constraintWeights.length === 0" class="muted-note">Веса не настроены.</div>
              <div class="weights-list">
                <div v-for="c in constraintWeights" :key="c.id" class="weight-item">
                  <div class="w-label">{{ constraintLabels[c.constraintType] || c.constraintType }}</div>
                  <div class="w-controls">
                    <input type="number" min="0" v-model.number="c.penalty" class="w-input" />
                    <button class="w-save" @click="saveConstraint(c)"><Save :size="14" /></button>
                  </div>
                </div>
              </div>
            </template>
          </div>
        </div>

        <div class="objects-main-content">
          <template v-if="activeObjectTab === 'audiences'">
            <div v-if="isCreatingRoom" class="room-form-wrapper">
              <header class="form-header">
                <div class="form-icon-box bg-purple-light"><MapPin :size="24" color="#a855f7" /></div>
                <div class="form-title-group">
                  <h3>{{ roomForm.id ? 'Редактирование аудитории' : 'Новая аудитория' }}</h3>
                  <p>Заполните параметры аудитории</p>
                </div>
              </header>
              <div class="form-grid">
                <div class="form-group"><label>НОМЕР / НАЗВАНИЕ</label>
                  <input type="text" v-model="roomForm.name" placeholder="например, 305" class="custom-input" />
                </div>
                <div class="form-group"><label>КОРПУС</label>
                  <select class="custom-select" v-model="roomForm.buildingId">
                    <option value="" disabled>Выберите корпус</option>
                    <option v-for="b in buildings" :key="b.id" :value="b.id">{{ b.name }}</option>
                  </select>
                </div>
                <div class="form-group"><label>ВМЕСТИМОСТЬ (МЕСТ)</label>
                  <input type="number" min="1" v-model.number="roomForm.capacity" class="custom-input" />
                </div>
              </div>
              <div class="form-actions">
                <button class="cancel-btn" @click="isCreatingRoom = false">Отмена</button>
                <button class="save-btn" @click="saveRoom"><Save :size="16" /> Сохранить</button>
              </div>
            </div>
            <div v-else class="empty-state-wrapper">
              <div class="empty-state">
                <div class="empty-icon-box bg-purple-light"><MapPin :size="40" color="#a855f7" /></div>
                <h3>Управление аудиториями</h3>
                <p>Выберите аудиторию для редактирования или<br>создайте новую.</p>
              </div>
            </div>
          </template>

          <template v-else-if="activeObjectTab === 'equipment'">
            <div class="empty-state-wrapper">
              <div class="empty-state">
                <div class="empty-icon-box bg-blue-light"><Wrench :size="40" color="#3b82f6" /></div>
                <h3>Справочник оборудования</h3>
                <p>Добавляйте новые типы оборудования. Они<br>будут доступны при настройке аудиторий и<br>ограничений нагрузки.</p>
                <span class="info-text">{{ equipments.length }} типов оборудования в системе</span>
              </div>
            </div>
          </template>

          <template v-else>
            <div class="empty-state-wrapper">
              <div class="empty-state">
                <div class="empty-icon-box"><SlidersHorizontal :size="40" color="#94a3b8" /></div>
                <h3>Веса мягких ограничений</h3>
                <p>Настройте, насколько сильно солвер штрафует<br>нарушение каждого мягкого ограничения.</p>
              </div>
            </div>
          </template>
        </div>
      </template>

      <!-- ===== Доступность преподавателя / аудитории ===== -->
      <div v-else-if="selected && (selected.kind === 'teacher' || selected.kind === 'room')" class="constraint-panel">
        <header class="cp-header">
          <div class="cp-icon" :class="selected.kind === 'teacher' ? 'bg-green-light' : 'bg-purple-light'">
            <User v-if="selected.kind === 'teacher'" :size="22" color="#10b981" />
            <MapPin v-else :size="22" color="#a855f7" />
          </div>
          <div><h3>{{ selected.name }}</h3><p>{{ selected.sublabel }}</p></div>
        </header>

        <div class="cp-section-title"><Clock :size="15" /> Доступность по времени</div>
        <p class="cp-hint">Клик по ячейке меняет градацию. Зажмите и проведите для заливки нескольких слотов.</p>
        <TimeGrid v-model="availabilityGrid" />

        <div class="cp-actions">
          <span v-if="panelError" class="cp-error">{{ panelError }}</span>
          <span v-else-if="panelStatus === 'saved'" class="cp-saved">✓ Сохранено</span>
          <button class="save-btn" :disabled="panelStatus === 'saving'" @click="savePanel">
            <Save :size="16" /> {{ panelStatus === 'saving' ? 'Сохранение…' : 'Сохранить' }}
          </button>
        </div>
      </div>

      <!-- ===== Смена группы ===== -->
      <div v-else-if="selected && selected.kind === 'group'" class="constraint-panel narrow">
        <header class="cp-header">
          <div class="cp-icon bg-teal-light"><Users :size="22" color="#14b8a6" /></div>
          <div><h3>{{ selected.name }}</h3><p>{{ selected.sublabel }}</p></div>
        </header>

        <div class="cp-section-title"><Layers :size="15" /> Смена обучения</div>
        <p class="cp-hint">Определяет, в какие пары можно ставить занятия группы.</p>
        <div class="shift-options">
          <label v-for="(label, val) in shiftLabels" :key="val" class="shift-option" :class="{ active: groupShift === val }">
            <input type="radio" :value="val" v-model="groupShift" />
            <span>{{ label }}</span>
          </label>
        </div>

        <div class="cp-actions">
          <span v-if="panelError" class="cp-error">{{ panelError }}</span>
          <span v-else-if="panelStatus === 'saved'" class="cp-saved">✓ Сохранено</span>
          <button class="save-btn" :disabled="panelStatus === 'saving'" @click="savePanel">
            <Save :size="16" /> {{ panelStatus === 'saving' ? 'Сохранение…' : 'Сохранить' }}
          </button>
        </div>
      </div>

      <!-- ===== По-нагрузочные ограничения ===== -->
      <div v-else-if="selected && selected.kind === 'curriculum'" class="constraint-panel narrow">
        <header class="cp-header">
          <div class="cp-icon bg-blue-light"><BookOpen :size="22" color="#3b82f6" /></div>
          <div><h3>{{ selected.name }}</h3><p>{{ selected.sublabel }}</p></div>
        </header>

        <div class="cp-block">
          <div class="cp-block-head"><Layers :size="15" /> Параллельность</div>
          <div class="switch-row">
            <div><p class="sw-title">Параллельные занятия</p><p class="sw-desc">Подгруппы занимаются одновременно в разных аудиториях</p></div>
            <button class="toggle" :class="{ on: curConstraints.isParallel }" @click="curConstraints.isParallel = !curConstraints.isParallel"><span class="knob" /></button>
          </div>
        </div>

        <div class="cp-block">
          <div class="cp-block-head"><Copy :size="15" /> Двойная пара</div>
          <div class="switch-row">
            <div><p class="sw-title">Двойная пара</p><p class="sw-desc">Две пары подряд в один день</p></div>
            <button class="toggle" :class="{ on: curConstraints.isDouble }" @click="curConstraints.isDouble = !curConstraints.isDouble"><span class="knob" /></button>
          </div>
        </div>

        <div class="cp-block">
          <div class="cp-block-head"><Wrench :size="15" /> Необходимое оборудование</div>
          <div class="tags">
            <span v-for="id in curConstraints.requiredEquipmentIds" :key="id" class="tag">
              {{ equipmentName(id) }}
              <X :size="12" class="tag-x" @click="removeEquipment(id)" />
            </span>
            <span v-if="!curConstraints.requiredEquipmentIds.length" class="muted-note small">Не требуется</span>
          </div>
          <select v-model="equipmentToAdd" class="custom-select" @change="addEquipment">
            <option value="">Добавить оборудование…</option>
            <option v-for="e in availableEquipment" :key="e.id" :value="e.id">{{ e.name }}</option>
          </select>
        </div>

        <div class="cp-block">
          <div class="cp-block-head"><Building2 :size="15" /> Предпочтительный корпус</div>
          <select v-model="curConstraints.preferredBuildingId" class="custom-select">
            <option :value="null">Без предпочтений</option>
            <option v-for="b in buildings" :key="b.id" :value="b.id">{{ b.name }}</option>
          </select>
        </div>

        <div class="cp-actions">
          <span v-if="panelError" class="cp-error">{{ panelError }}</span>
          <span v-else-if="panelStatus === 'saved'" class="cp-saved">✓ Сохранено</span>
          <button class="save-btn" :disabled="panelStatus === 'saving'" @click="savePanel">
            <Save :size="16" /> {{ panelStatus === 'saving' ? 'Сохранение…' : 'Сохранить' }}
          </button>
        </div>
      </div>

      <!-- ===== Пустое состояние ===== -->
      <div v-else class="empty-state">
        <div class="empty-icon-box"><Settings2 :size="40" color="#94a3b8" /></div>
        <h3>Выберите объект</h3>
        <p>Выберите преподавателя, аудиторию, группу или<br>запись нагрузки для настройки ограничений.</p>
      </div>
    </main>

  </div>
</template>

<style scoped>
.settings-container {
  display: flex; height: calc(100vh - 60px); background-color: white;
  border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  overflow: hidden; font-family: sans-serif;
}

/* --- ЛЕВАЯ ПАНЕЛЬ --- */
.sidebar-panel {
  display: flex; flex-direction: column; width: max-content; min-width: 330px; max-width: 360px;
  flex-shrink: 0; border-right: 1px solid #f1f5f9; padding: 20px; overflow-y: auto;
}
.sidebar-panel::-webkit-scrollbar { width: 6px; }
.sidebar-panel::-webkit-scrollbar-thumb { background-color: #cbd5e1; border-radius: 4px; }

.panel-header { display: flex; align-items: center; gap: 10px; margin-bottom: 20px; }
.panel-header h2 { font-size: 18px; font-weight: 600; color: #1e293b; margin: 0; }
.search-box { margin-bottom: 20px; }

.object-tabs { display: flex; justify-content: space-between; border-bottom: 1px solid #f1f5f9; padding-bottom: 12px; margin-bottom: 16px; }
.obj-tab { display: flex; flex-direction: column; align-items: center; gap: 6px; color: #94a3b8; font-size: 11px; font-weight: 500; cursor: pointer; transition: color 0.2s; }
.obj-tab:hover { color: #64748b; }
.obj-tab.active { color: #1a4d9c; position: relative; }
.obj-tab.active::after { content: ''; position: absolute; bottom: -13px; left: 0; right: 0; height: 2px; background-color: #1a4d9c; border-radius: 2px 2px 0 0; }

.list-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.list-title { font-size: 13px; font-weight: 600; color: #64748b; }
.badge { background-color: #f1f5f9; color: #64748b; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; }

.tree-view { display: flex; flex-direction: column; gap: 8px; padding-right: 8px; }
.tree-node { display: flex; flex-direction: column; }
.node-header { display: flex; align-items: center; gap: 8px; padding: 6px 0; cursor: pointer; user-select: none; }
.chevron-icon { transition: transform 0.2s ease; flex-shrink: 0; }
.chevron-icon.is-closed { transform: rotate(-90deg); }
.node-text { font-size: 13px; font-weight: 500; color: #334155; white-space: nowrap; }
.node-children { padding-left: 20px; display: flex; flex-direction: column; gap: 4px; border-left: 2px solid #f1f5f9; margin-left: 7px; margin-top: 4px; }
.leaf-node { padding: 8px 12px; margin-left: 8px; border-radius: 8px; cursor: pointer; transition: background-color 0.2s; }
.leaf-node:hover { background-color: #f8fafc; }
.leaf-node.selected { background-color: #eff6ff; box-shadow: inset 0 0 0 1px #bfdbfe; }
.leaf-title { font-size: 13px; font-weight: 500; color: #1e293b; margin-bottom: 2px; white-space: nowrap; }
.leaf-subtitle { font-size: 11px; color: #94a3b8; white-space: nowrap; }

.load-card-list { display: flex; flex-direction: column; gap: 8px; padding-right: 8px; }
.load-card { display: flex; flex-direction: column; gap: 6px; padding: 12px; border: 1px solid #f1f5f9; border-radius: 10px; cursor: pointer; transition: all 0.2s; }
.load-card:hover { border-color: #cbd5e1; }
.load-card.selected { background-color: #eff6ff; border-color: #bfdbfe; }
.load-title { font-size: 14px; font-weight: 600; color: #1e293b; }
.load-subtitle { font-size: 12px; color: #94a3b8; }
.load-meta { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; margin-top: 2px; }
.type-badge { padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: 500; background-color: #f1f5f9; color: #64748b; }
.badge-practice { background-color: #ccfbf1; color: #0f766e; }
.badge-lab { background-color: #f3e8ff; color: #7e22ce; }
.badge-lecture { background-color: #e0f2fe; color: #0369a1; }
.load-group { font-size: 11px; color: #64748b; font-weight: 500; }

.objects-summary { display: flex; flex-direction: column; align-items: center; padding: 40px 0; text-align: center; }
.summary-icon { margin-bottom: 16px; }
.summary-title { font-size: 13px; color: #64748b; margin-bottom: 24px; }
.summary-row { display: flex; justify-content: space-between; width: 100%; font-size: 13px; color: #475569; margin-bottom: 12px; }
.summary-row b { color: #1e293b; }

/* --- ПРАВАЯ ЧАСТЬ --- */
.main-workspace { flex-grow: 1; background-color: #fafbfc; display: flex; justify-content: center; align-items: center; overflow-y: auto; }
.main-workspace.is-objects-mode { align-items: stretch; justify-content: flex-start; background-color: white; flex-direction: row; }

.empty-state { display: flex; flex-direction: column; align-items: center; text-align: center; }
.empty-icon-box { width: 70px; height: 70px; border-radius: 16px; display: flex; justify-content: center; align-items: center; margin-bottom: 20px; background-color: #e2e8f0; }
.empty-state h3 { font-size: 18px; color: #1e293b; margin: 0 0 12px 0; font-weight: 600; }
.empty-state p { font-size: 14px; color: #64748b; line-height: 1.5; margin: 0 0 16px 0; }
.info-text { font-size: 12px; color: #94a3b8; }
.bg-purple-light { background-color: #f3e8ff; }
.bg-blue-light { background-color: #e0f2fe; }
.bg-green-light { background-color: #dcfce7; }
.bg-teal-light { background-color: #ccfbf1; }

/* === ПАНЕЛЬ ОГРАНИЧЕНИЙ === */
.constraint-panel { width: 100%; max-width: 720px; padding: 36px; align-self: flex-start; }
.constraint-panel.narrow { max-width: 520px; }
.cp-header { display: flex; align-items: center; gap: 16px; margin-bottom: 28px; }
.cp-icon { width: 46px; height: 46px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.cp-header h3 { font-size: 18px; font-weight: 600; color: #1e293b; margin: 0 0 3px 0; }
.cp-header p { font-size: 13px; color: #64748b; margin: 0; }
.cp-section-title { display: flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 600; color: #334155; margin-bottom: 6px; }
.cp-hint { font-size: 12px; color: #94a3b8; margin: 0 0 16px 0; }

.cp-block { padding: 18px 0; border-bottom: 1px solid #f1f5f9; }
.cp-block:first-of-type { padding-top: 0; }
.cp-block-head { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 12px; }

.switch-row { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.sw-title { font-size: 13px; font-weight: 500; color: #1e293b; margin: 0; }
.sw-desc { font-size: 11px; color: #94a3b8; margin: 2px 0 0 0; }
.toggle { position: relative; width: 44px; height: 24px; border-radius: 999px; border: none; background-color: #e2e8f0; cursor: pointer; transition: background-color 0.2s; flex-shrink: 0; }
.toggle.on { background-color: #1a4d9c; }
.toggle .knob { position: absolute; top: 2px; left: 2px; width: 20px; height: 20px; border-radius: 50%; background: white; box-shadow: 0 1px 3px rgba(0,0,0,0.2); transition: transform 0.2s; }
.toggle.on .knob { transform: translateX(20px); }

.tags { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 12px; }
.tag { display: inline-flex; align-items: center; gap: 6px; background-color: #eff6ff; color: #1d4ed8; border: 1px solid #bfdbfe; border-radius: 999px; padding: 4px 10px; font-size: 12px; font-weight: 500; }
.tag-x { cursor: pointer; opacity: 0.7; }
.tag-x:hover { opacity: 1; }

.shift-options { display: flex; flex-direction: column; gap: 10px; margin-bottom: 8px; }
.shift-option { display: flex; align-items: center; gap: 10px; padding: 12px 14px; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-size: 14px; color: #334155; transition: all 0.2s; }
.shift-option:hover { border-color: #cbd5e1; }
.shift-option.active { border-color: #1a4d9c; background-color: #eff6ff; color: #1a4d9c; font-weight: 500; }
.shift-option input { accent-color: #1a4d9c; }

.cp-actions { display: flex; align-items: center; justify-content: flex-end; gap: 14px; margin-top: 24px; }
.cp-error { font-size: 13px; color: #dc2626; }
.cp-saved { font-size: 13px; color: #16a34a; font-weight: 500; }

.custom-input, .custom-select { width: 100%; height: 44px; border: 1px solid #cbd5e1; border-radius: 8px; padding: 0 14px; font-size: 14px; color: #1e293b; outline: none; background-color: white; transition: border-color 0.2s; }
.custom-input:focus, .custom-select:focus { border-color: #1d4ed8; }
.custom-input::placeholder { color: #94a3b8; }

/* === Центральная колонка «Объекты» === */
.objects-sub-sidebar { width: 340px; border-right: 1px solid #e2e8f0; display: flex; flex-direction: column; }
.workspace-tabs { display: flex; border-bottom: 1px solid #e2e8f0; }
.wk-tab { display: flex; align-items: center; justify-content: center; flex: 1; gap: 8px; padding: 16px 0; font-size: 13px; font-weight: 600; color: #64748b; cursor: pointer; border-bottom: 2px solid transparent; transition: all 0.2s; }
.wk-tab:hover { color: #1e293b; }
.wk-tab.active { color: #1d4ed8; border-bottom-color: #1d4ed8; }
.sub-sidebar-content { padding: 20px; overflow-y: auto; flex-grow: 1; }
.sub-sidebar-content::-webkit-scrollbar { width: 6px; }
.sub-sidebar-content::-webkit-scrollbar-thumb { background-color: #cbd5e1; border-radius: 4px; }

.create-btn { display: flex; align-items: center; justify-content: center; gap: 8px; background-color: #1d4ed8; color: white; border: none; border-radius: 8px; padding: 12px; font-size: 14px; font-weight: 500; cursor: pointer; margin-bottom: 24px; transition: background-color 0.2s; width: 100%; }
.create-btn:hover { background-color: #1e40af; }
.management-list { display: flex; flex-direction: column; gap: 12px; }
.management-item { display: flex; justify-content: space-between; align-items: flex-start; padding: 12px 16px; border: 1px solid #f1f5f9; border-radius: 12px; transition: border-color 0.2s; cursor: pointer; }
.management-item:hover { border-color: #cbd5e1; }
.m-title { font-size: 13px; font-weight: 600; color: #1e293b; margin-bottom: 4px; }
.m-desc { font-size: 11px; color: #94a3b8; }
.delete-btn { background: none; border: none; color: #cbd5e1; cursor: pointer; padding: 4px; border-radius: 8px; transition: all 0.2s; margin-top: -2px; }
.delete-btn:hover { color: #dc2626; background-color: #fef2f2; }

.add-equipment-form { display: flex; gap: 8px; margin-bottom: 24px; }
.eq-input { flex-grow: 1; border: 1px solid #e2e8f0; border-radius: 8px; padding: 0 12px; font-size: 13px; outline: none; transition: border-color 0.2s; }
.eq-input:focus { border-color: #1d4ed8; }
.add-eq-btn { background-color: #1d4ed8; color: white; border: none; border-radius: 8px; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; cursor: pointer; flex-shrink: 0; transition: background-color 0.2s; }
.add-eq-btn:hover { background-color: #1e40af; }

.section-label { font-size: 10px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 12px; letter-spacing: 0.05em; }
.equipment-list { display: flex; flex-direction: column; gap: 4px; }
.equipment-item { display: flex; align-items: center; gap: 12px; padding: 8px 12px; font-size: 13px; color: #475569; cursor: pointer; border-radius: 8px; transition: background-color 0.2s; }
.equipment-item:hover { background-color: #f8fafc; color: #1e293b; }
.eq-name { flex-grow: 1; }
.eq-del { margin-left: auto; }

.objects-main-content { flex-grow: 1; background-color: #fafbfc; display: flex; flex-direction: column; overflow-y: auto; }
.empty-state-wrapper { flex-grow: 1; display: flex; align-items: center; justify-content: center; padding: 40px; }

.room-form-wrapper { padding: 40px; width: 100%; max-width: 800px; }
.form-header { display: flex; align-items: center; gap: 16px; margin-bottom: 32px; }
.form-icon-box { width: 48px; height: 48px; border-radius: 12px; display: flex; justify-content: center; align-items: center; }
.form-title-group h3 { font-size: 18px; font-weight: 600; color: #1e293b; margin: 0 0 4px 0; }
.form-title-group p { font-size: 13px; color: #64748b; margin: 0; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px; }
.form-group label { display: block; font-size: 11px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 8px; letter-spacing: 0.05em; }

.save-btn { display: flex; align-items: center; gap: 8px; background-color: #1d4ed8; color: white; padding: 12px 24px; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 14px; transition: background-color 0.2s; }
.save-btn:hover { background-color: #1e40af; }
.save-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.form-actions { display: flex; gap: 12px; margin-top: 12px; }
.cancel-btn { background: #e2e8f0; color: #475569; padding: 12px 24px; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 14px; }
.cancel-btn:hover { background: #cbd5e1; }

.objects-error { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; border-radius: 8px; padding: 10px 12px; font-size: 13px; margin-bottom: 16px; }
.muted-note { color: #94a3b8; font-size: 13px; padding: 8px 0; }
.muted-note.small { font-size: 12px; padding: 4px 0 4px 8px; }

.weights-list { display: flex; flex-direction: column; gap: 12px; }
.weight-item { display: flex; flex-direction: column; gap: 8px; padding: 12px; border: 1px solid #f1f5f9; border-radius: 12px; }
.w-label { font-size: 13px; font-weight: 600; color: #1e293b; }
.w-controls { display: flex; gap: 8px; }
.w-input { flex-grow: 1; height: 36px; border: 1px solid #cbd5e1; border-radius: 8px; padding: 0 12px; font-size: 14px; outline: none; }
.w-input:focus { border-color: #1d4ed8; }
.w-save { width: 40px; background: #1d4ed8; color: white; border: none; border-radius: 8px; display: flex; align-items: center; justify-content: center; cursor: pointer; flex-shrink: 0; }
.w-save:hover { background: #1e40af; }
</style>
