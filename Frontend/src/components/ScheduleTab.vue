<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  Calendar, RotateCcw, Sparkles, ChevronDown, ChevronUp, Download,
  User, Users, MapPin, Plus, CheckCircle2, GraduationCap, Building2,
  Moon, Sun, X, Save, Trash2, ArrowLeftRight,
} from 'lucide-vue-next'
import BaseButton from './BaseButton.vue'
import { lookups } from '../api/lookups'
import { lessons } from '../api/lessons'
import { calendar } from '../api/calendar'
import { management } from '../api/management'
import { ApiError } from '../api/http'
import { useToast } from '../composables/useToast'
import type {
  InstituteDto, DepartmentDto, TeacherDto, DegreeDto, CourseDto, GroupDto, BuildingDto, RoomDto,
  SemesterDto, WeekDto, LessonDTO, DomainLessonType, TimeSlotDto, CurriculumOptionDto,
  GenerationJobStatus,
} from '../api/types'

type Entity = 'teachers' | 'groups' | 'rooms'
const currentEntity = ref<Entity>('teachers')
const toast = useToast()

const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб']
const timeSlots = [
  { id: 1, start: '08:00', end: '09:30' }, { id: 2, start: '09:40', end: '11:10' },
  { id: 3, start: '11:20', end: '12:50' }, { id: 4, start: '13:20', end: '14:50' },
  { id: 5, start: '15:00', end: '16:30' }, { id: 6, start: '16:40', end: '18:10' },
  { id: 7, start: '18:20', end: '19:50' }, { id: 8, start: '20:00', end: '21:30' },
]

// Названия и цвета по типу занятия (как в макете Figma).
const lessonTypeMeta: Record<DomainLessonType, { label: string; cls: string }> = {
  Lecture: { label: 'Лекция', cls: 'lt-lecture' },
  Seminar: { label: 'Практика', cls: 'lt-practice' },
  Laboratory: { label: 'Лаб.', cls: 'lt-lab' },
  Examination: { label: 'Экзамен', cls: 'lt-exam' },
  Consultation: { label: 'Консульт.', cls: 'lt-consult' },
}

// --- Календарь ---
const semesters = ref<SemesterDto[]>([])
const selSemester = ref('')
const weeks = ref<WeekDto[]>([])
const selectedWeekId = ref('')

const selectedWeek = computed(() => weeks.value.find(w => w.id === selectedWeekId.value) ?? null)
const weekType = computed<'red' | 'blue' | null>(() => {
  if (!selectedWeek.value) return null
  return selectedWeek.value.weekType === 'Red' ? 'red' : 'blue'
})
// Текущая неделя — та, чей диапазон дат включает сегодня.
const todayIso = new Date().toISOString().slice(0, 10)
const isCurrentWeek = (w: WeekDto) => w.startDate <= todayIso && todayIso <= w.endDate

// --- Справочники ---
const institutes = ref<InstituteDto[]>([])
const departments = ref<DepartmentDto[]>([])
const teachers = ref<TeacherDto[]>([])
const degrees = ref<DegreeDto[]>([])
const courses = ref<CourseDto[]>([])
const groups = ref<GroupDto[]>([])
const buildings = ref<BuildingDto[]>([])
const rooms = ref<RoomDto[]>([])

const selInstitute = ref('')
const selDept = ref('')
const selTeacher = ref('')
const selDegree = ref('')
const selCourse = ref('')
const selGroup = ref('')
const selBuilding = ref('')
const selRoom = ref('')

const selectedLeafId = computed(() => {
  if (currentEntity.value === 'teachers') return selTeacher.value
  if (currentEntity.value === 'groups') return selGroup.value
  return selRoom.value
})
const hasSelection = computed(() => selectedLeafId.value !== '')

// --- Занятия выбранного объекта ---
const lessonList = ref<LessonDTO[]>([])
const lessonsLoading = ref(false)
const banner = ref('')
const isAutoGenerateOpen = ref(false)

// Карта «день-пара» → занятие для быстрой раскладки в сетке.
const lessonsByCell = computed<Record<string, LessonDTO>>(() => {
  const map: Record<string, LessonDTO> = {}
  for (const l of lessonList.value) {
    const key = `${l.dayOfWeek}-${l.pairNumber}`
    if (!map[key]) map[key] = l // при коллизии показываем первое (параллельные потоки)
  }
  return map
})
const cellAt = (dayIdx: number, pair: number): LessonDTO | undefined =>
  lessonsByCell.value[`${dayIdx}-${pair}`]

async function loadLessons() {
  banner.value = ''
  if (!hasSelection.value) { lessonList.value = []; return }
  lessonsLoading.value = true
  try {
    const entity = currentEntity.value === 'teachers' ? 'teacher'
      : currentEntity.value === 'groups' ? 'group' : 'room'
    lessonList.value = await lessons.byEntity(entity, selectedLeafId.value, selectedWeekId.value || undefined)
  } catch (e) {
    banner.value = e instanceof ApiError ? e.message : 'Не удалось загрузить занятия.'
    lessonList.value = []
  } finally {
    lessonsLoading.value = false
  }
}

// ───────── Редактор расписания (добавление / удаление / перемещение пары) ─────────

const allRooms = ref<RoomDto[]>([])           // все аудитории для выбора в форме
const timeslots = ref<TimeSlotDto[]>([])       // слоты выбранной недели
const timeslotMap = computed<Record<string, string>>(() => {
  const m: Record<string, string> = {}
  for (const t of timeslots.value) m[`${t.dayOfWeek}-${t.number}`] = t.id
  return m
})
const timeSlotIdFor = (dayIdx: number, pair: number): string | undefined =>
  timeslotMap.value[`${dayIdx}-${pair}`]

// Выбор/перемещение существующей пары.
const selectedLesson = ref<LessonDTO | null>(null)
const moveMode = ref(false)
const movingLesson = ref<LessonDTO | null>(null)

// Панель добавления.
const editOpen = ref(false)
const editDay = ref(0)
const editPair = ref(1)
const editCurriculumId = ref('')
const editRoomId = ref('')
const curriculumOptions = ref<CurriculumOptionDto[]>([])
const saving = ref(false)

const editFormValid = computed(() =>
  !!editCurriculumId.value && !!editRoomId.value && !!timeSlotIdFor(editDay.value, editPair.value))

async function loadCurriculumOptions() {
  const params: { groupId?: string; teacherId?: string; semesterId?: string } = {
    semesterId: selSemester.value || undefined,
  }
  if (currentEntity.value === 'teachers' && selTeacher.value) params.teacherId = selTeacher.value
  if (currentEntity.value === 'groups' && selGroup.value) params.groupId = selGroup.value
  curriculumOptions.value = await calendar.curriculums(params).catch(() => [])
}

function openAddPanel(dayIdx = 0, pair = 1) {
  selectedLesson.value = null
  moveMode.value = false
  movingLesson.value = null
  editDay.value = dayIdx
  editPair.value = pair
  editCurriculumId.value = ''
  // В режиме «аудитории» предзаполняем выбранной аудиторией.
  editRoomId.value = currentEntity.value === 'rooms' ? selRoom.value : ''
  editOpen.value = true
  loadCurriculumOptions()
}

function closePanel() {
  editOpen.value = false
}

async function savePair() {
  if (!editFormValid.value || saving.value) return
  const opt = curriculumOptions.value.find(c => c.id === editCurriculumId.value)
  const timeSlotId = timeSlotIdFor(editDay.value, editPair.value)
  if (!opt || !timeSlotId) return
  saving.value = true
  banner.value = ''
  try {
    await lessons.create({
      classroomId: editRoomId.value,
      timeSlotId,
      streamId: opt.streamId,
      semesterId: selSemester.value,
      curriculumId: opt.id,
    })
    editOpen.value = false
    toast.success('Пара добавлена.')
    await loadLessons()
  } catch (e) {
    // 409-коллизия (аудитория/преподаватель/группа) приходит сюда читаемым сообщением.
    toast.error(e instanceof ApiError ? e.message : 'Не удалось сохранить пару.')
  } finally {
    saving.value = false
  }
}

function selectLesson(l: LessonDTO) {
  if (moveMode.value) return
  selectedLesson.value = selectedLesson.value?.id === l.id ? null : l
  editOpen.value = false
}

async function deleteSelected() {
  if (!selectedLesson.value) return
  if (!confirm('Удалить эту пару?')) return
  banner.value = ''
  try {
    await lessons.remove(selectedLesson.value.id)
    selectedLesson.value = null
    toast.success('Пара удалена.')
    await loadLessons()
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Не удалось удалить пару.')
  }
}

function startMove() {
  if (!selectedLesson.value) return
  moveMode.value = true
  movingLesson.value = selectedLesson.value
  selectedLesson.value = null
}

function cancelMove() {
  moveMode.value = false
  movingLesson.value = null
}

async function onCellClick(dayIdx: number, pair: number) {
  const existing = cellAt(dayIdx, pair)
  if (moveMode.value && movingLesson.value) {
    if (existing) return // занятая ячейка — переместить нельзя
    const targetSlot = timeSlotIdFor(dayIdx, pair)
    if (!targetSlot) { banner.value = 'Для этой ячейки нет временного слота в выбранной неделе.'; return }
    const src = movingLesson.value
    banner.value = ''
    try {
      await lessons.create({
        classroomId: src.classroomId,
        timeSlotId: targetSlot,
        streamId: src.streamId,
        semesterId: selSemester.value,
        curriculumId: src.curriculumId ?? undefined,
      })
      await lessons.remove(src.id)
      cancelMove()
      toast.success('Пара перемещена.')
      await loadLessons()
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Не удалось переместить пару.')
    }
    return
  }
  if (existing) { selectLesson(existing); return }
  openAddPanel(dayIdx, pair)
}

// --- Каскады справочников ---
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

function resetEditing() {
  selectedLesson.value = null
  moveMode.value = false
  movingLesson.value = null
  editOpen.value = false
}

// Перезагрузка занятий при смене объекта или недели.
watch(selectedLeafId, () => { resetEditing(); loadLessons() })
watch(currentEntity, resetEditing)
watch(selectedWeekId, async (id) => {
  resetEditing()
  timeslots.value = id ? await calendar.timeslots(id).catch(() => []) : []
  await loadLessons()
})

// Загрузка недель при выборе семестра.
watch(selSemester, async (id) => {
  weeks.value = []; selectedWeekId.value = ''
  if (!id) return
  weeks.value = await calendar.weeks(id).catch(() => [])
  // По умолчанию выбираем текущую неделю, иначе первую.
  const current = weeks.value.find(isCurrentWeek)
  selectedWeekId.value = current?.id ?? weeks.value[0]?.id ?? ''
})

// Поллинг статуса фоновой генерации до завершения (с мягким таймаутом ~200 с).
const sleep = (ms: number) => new Promise<void>(resolve => setTimeout(resolve, ms))

async function pollGeneration(jobId: string): Promise<GenerationJobStatus | null> {
  const deadline = Date.now() + 200_000
  while (Date.now() < deadline) {
    await sleep(1500)
    const s = await lessons.generationStatus(jobId)
    if (s.state === 'Succeeded' || s.state === 'Failed') return s
  }
  return null
}

// Поставить генерацию одного института в очередь и дождаться результата (текстовая сводка).
async function runInstituteGeneration(instituteId: string): Promise<string> {
  // Генерация идёт в фоне — HTTP-запрос не висит до 180 с.
  const job = await lessons.generateForInstituteAsync(selSemester.value, instituteId)
  const final = await pollGeneration(job.jobId)
  if (!final) return 'выполняется дольше обычного — загляните позже'
  if (final.state === 'Succeeded' && final.result) {
    const r = final.result
    return `${r.status}, занятий: ${r.lessonsCreated}, штраф: ${r.objectiveValue}, ` +
      `время: ${r.wallTimeSeconds.toFixed(1)} с`
  }
  return `не удалась: ${final.error ?? final.state}`
}

async function generate(scope: 'university' | 'institute') {
  isAutoGenerateOpen.value = false
  if (!selSemester.value) { banner.value = 'Выберите семестр для генерации.'; return }

  if (scope === 'institute') {
    if (!selInstitute.value) { banner.value = 'Выберите институт (в фильтрах) для генерации по институту.'; return }
    banner.value = ''
    lessonsLoading.value = true
    try {
      banner.value = 'Генерация запущена в фоне, ожидаем результат…'
      banner.value = `Генерация: ${await runInstituteGeneration(selInstitute.value)}.`
      await loadLessons()
    } catch (e) {
      banner.value = e instanceof ApiError ? e.message : 'Ошибка генерации.'
    } finally {
      lessonsLoading.value = false
    }
    return
  }

  // scope === 'university': последовательно по всем институтам. Декомпозиция —
  // каждый следующий институт учитывает ресурсы, уже занятые предыдущими.
  if (institutes.value.length === 0) { banner.value = 'Список институтов пуст — обновите страницу.'; return }
  banner.value = ''
  lessonsLoading.value = true
  try {
    const total = institutes.value.length
    let done = 0
    for (const inst of institutes.value) {
      banner.value = `Генерация по вузу: «${inst.name}» (${done + 1}/${total})…`
      const res = await runInstituteGeneration(inst.id)
      done++
      banner.value = `«${inst.name}»: ${res}. (${done}/${total})`
    }
    banner.value = `Генерация по вузу завершена: обработано институтов — ${total}.`
    await loadLessons()
  } catch (e) {
    banner.value = e instanceof ApiError ? e.message : 'Ошибка генерации по вузу.'
  } finally {
    lessonsLoading.value = false
  }
}

// Выгрузить (опубликовать) черновик выбранного института: Draft -> Current.
async function publish() {
  if (!selInstitute.value) { banner.value = 'Выберите институт (в фильтрах) для выгрузки расписания.'; return }
  if (!confirm('Выгрузить расписание института? Текущее опубликованное расписание будет заменено черновиком.')) return
  banner.value = ''
  lessonsLoading.value = true
  try {
    const r = await lessons.publishInstitute(selInstitute.value)
    toast.success(`Расписание выгружено: опубликовано занятий — ${r.published}.`)
    await loadLessons()
  } catch (e) {
    toast.error(e instanceof ApiError
      ? (e.status === 404 ? 'У института нет черновика для выгрузки.' : e.message)
      : 'Не удалось выгрузить расписание.')
  } finally {
    lessonsLoading.value = false
  }
}

// Сбросить до выгруженного: удалить черновик выбранного института, оставив опубликованное.
async function discard() {
  if (!selInstitute.value) { banner.value = 'Выберите институт (в фильтрах) для сброса черновика.'; return }
  if (!confirm('Сбросить черновик до выгруженного расписания? Несохранённые изменения института будут удалены.')) return
  banner.value = ''
  lessonsLoading.value = true
  try {
    const r = await lessons.discardInstitute(selInstitute.value)
    if (r.discarded > 0) toast.success(`Черновик сброшен: удалено занятий — ${r.discarded}.`)
    else toast.info('Черновика нет — сбрасывать нечего.')
    await loadLessons()
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Не удалось сбросить черновик.')
  } finally {
    lessonsLoading.value = false
  }
}

onMounted(async () => {
  institutes.value = await lookups.institutes().catch(() => [])
  buildings.value = await lookups.buildings().catch(() => [])
  // Полный список аудиторий для выбора в форме (lookups.rooms ограничен 20 записями).
  allRooms.value = await management.classrooms().catch(() => [])
  semesters.value = await calendar.semesters().catch(() => [])
  // По умолчанию — текущий семестр, иначе первый (самый свежий).
  const cur = semesters.value.find(s => s.isCurrent)
  selSemester.value = cur?.id ?? semesters.value[0]?.id ?? ''
})
</script>

<template>
  <div class="schedule-container">

    <!-- 1. ВЕРХНЯЯ ПАНЕЛЬ -->
    <header class="top-bar">
      <div class="left-controls">
        <Calendar :size="18" color="#64748b" />
        <span class="label">Семестр:</span>
        <select class="select-dropdown sem-select" :class="{ 'is-placeholder': selSemester === '' }" v-model="selSemester">
          <option value="" disabled hidden>Семестр</option>
          <option v-for="s in semesters" :key="s.id" :value="s.id">
            {{ s.startDate }} — {{ s.endDate }}{{ s.isCurrent ? ' (текущий)' : '' }}
          </option>
        </select>

        <span class="label">Неделя:</span>
        <select class="select-dropdown week-select" :class="{ 'is-placeholder': selectedWeekId === '' }"
                v-model="selectedWeekId" :disabled="!selSemester">
          <option value="" disabled hidden>Неделя</option>
          <option v-for="w in weeks" :key="w.id" :value="w.id">
            {{ w.number }} неделя{{ isCurrentWeek(w) ? ' ★' : '' }}
          </option>
        </select>

        <div v-if="weekType" class="week-badge" :class="weekType === 'red' ? 'week-red' : 'week-blue'">
          <Moon v-if="weekType === 'red'" :size="14" />
          <Sun v-else :size="14" />
          {{ weekType === 'red' ? 'Красная неделя' : 'Синяя неделя' }}
        </div>

        <div v-if="selectedWeek && isCurrentWeek(selectedWeek)" class="badge badge-green">
          <CheckCircle2 :size="14" /> Текущая
        </div>
      </div>

      <div class="right-controls">
        <BaseButton variant="outline" :disabled="lessonsLoading" @click="discard">
          <RotateCcw :size="16" /> Сбросить до выгруженного
        </BaseButton>
        <div class="dropdown-wrapper">
          <BaseButton variant="gradient" @click="isAutoGenerateOpen = !isAutoGenerateOpen">
            <Sparkles :size="16" /> Автогенерация
            <ChevronUp v-if="isAutoGenerateOpen" :size="16" />
            <ChevronDown v-else :size="16" />
          </BaseButton>

          <div v-if="isAutoGenerateOpen" class="dropdown-menu">
            <div class="dropdown-item" @click="generate('institute')">
              <Building2 :size="18" class="dd-icon" /><span>Для выбранного института</span>
            </div>
            <div class="dropdown-item" @click="generate('university')">
              <GraduationCap :size="18" class="dd-icon" /><span>Для всего университета (по институтам)</span>
            </div>
          </div>
        </div>
        <BaseButton variant="outline" :disabled="lessonsLoading" @click="publish">
          <Download :size="16" /> Выгрузить
        </BaseButton>
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

      <div class="toolbar">
        <template v-if="moveMode">
          <span class="move-hint"><ArrowLeftRight :size="14" /> Выберите пустую ячейку</span>
          <button class="tool-btn" @click="cancelMove"><X :size="14" /> Отмена</button>
        </template>
        <template v-else-if="selectedLesson">
          <button class="tool-btn tool-danger" @click="deleteSelected"><Trash2 :size="14" /> Удалить пару</button>
          <button class="tool-btn tool-info" @click="startMove"><ArrowLeftRight :size="14" /> Переместить</button>
        </template>
        <BaseButton variant="primary" :disabled="!hasSelection || !selectedWeekId" @click="openAddPanel()">
          <Plus :size="16" /> Добавить пару
        </BaseButton>
      </div>
    </div>

    <!-- 4. СЕТКА ИЛИ ПУСТОЕ СОСТОЯНИЕ -->
    <div v-if="hasSelection" class="schedule-grid-container">

      <div v-if="weekType" class="week-indicator" :class="weekType === 'red' ? 'week-red' : 'week-blue'">
        <Moon v-if="weekType === 'red'" :size="14" />
        <Sun v-else :size="14" />
        {{ weekType === 'red' ? 'Красная неделя' : 'Синяя неделя' }}
        <span v-if="selectedWeek"> · Неделя {{ selectedWeek.number }} из {{ weeks.length }}</span>
      </div>

      <div v-if="banner" class="alert-banner is-error">{{ banner }}</div>
      <div v-else-if="lessonsLoading" class="alert-banner">Загрузка занятий…</div>
      <div v-else-if="!selectedWeekId" class="alert-banner">Выберите неделю, чтобы увидеть занятия.</div>
      <div v-else-if="lessonList.length === 0" class="alert-banner">На выбранную неделю занятий нет.</div>

      <div class="grid-and-panel">
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
          <template v-for="(day, dayIdx) in daysOfWeek" :key="day">
            <div v-if="cellAt(dayIdx, slot.id)" class="lesson-cell"
                 :class="[
                   cellAt(dayIdx, slot.id)!.lessonType ? lessonTypeMeta[cellAt(dayIdx, slot.id)!.lessonType as DomainLessonType].cls : 'lt-default',
                   { 'cell-selected': selectedLesson?.id === cellAt(dayIdx, slot.id)!.id, 'cell-dim': moveMode },
                 ]"
                 @click="onCellClick(dayIdx, slot.id)">
              <span class="lc-subject">{{ cellAt(dayIdx, slot.id)!.subjectName || 'Занятие' }}</span>
              <span v-if="currentEntity !== 'teachers' && cellAt(dayIdx, slot.id)!.teacherName" class="lc-line">
                {{ cellAt(dayIdx, slot.id)!.teacherName }}
              </span>
              <span v-if="currentEntity !== 'groups' && cellAt(dayIdx, slot.id)!.groupNames" class="lc-line">
                {{ cellAt(dayIdx, slot.id)!.groupNames }}
              </span>
              <span v-if="currentEntity !== 'rooms'" class="lc-line">Ауд. {{ cellAt(dayIdx, slot.id)!.classroomName }}</span>
              <span class="lc-type">
                {{ cellAt(dayIdx, slot.id)!.lessonType ? lessonTypeMeta[cellAt(dayIdx, slot.id)!.lessonType as DomainLessonType].label : '' }}
              </span>
            </div>
            <div v-else class="empty-cell"
                 :class="{ 'cell-movable': moveMode && timeSlotIdFor(dayIdx, slot.id) }"
                 @click="onCellClick(dayIdx, slot.id)">
              <Plus v-if="moveMode && timeSlotIdFor(dayIdx, slot.id)" :size="16" color="#16a34a" class="cell-plus" />
            </div>
          </template>
        </div>
      </div>

      <!-- Панель добавления пары -->
      <aside v-if="editOpen" class="edit-panel">
        <header class="ep-header">
          <div>
            <p class="ep-eyebrow">Добавление пары</p>
            <p class="ep-title">Новое занятие</p>
          </div>
          <button class="ep-close" @click="closePanel"><X :size="16" /></button>
        </header>

        <div class="ep-body">
          <div class="ep-field ep-grid2">
            <div>
              <label>День</label>
              <select class="select-dropdown ep-select" v-model.number="editDay">
                <option v-for="(d, i) in daysOfWeek" :key="d" :value="i">{{ d }}</option>
              </select>
            </div>
            <div>
              <label>Пара</label>
              <select class="select-dropdown ep-select" v-model.number="editPair">
                <option v-for="s in timeSlots" :key="s.id" :value="s.id">{{ s.id }} ({{ s.start }})</option>
              </select>
            </div>
          </div>

          <div v-if="!timeSlotIdFor(editDay, editPair)" class="ep-warn">
            Для этой ячейки нет временного слота в выбранной неделе.
          </div>

          <div class="ep-field">
            <label>Дисциплина · преподаватель · тип</label>
            <select class="select-dropdown ep-select" v-model="editCurriculumId">
              <option value="">Выберите учебный план…</option>
              <option v-for="c in curriculumOptions" :key="c.id" :value="c.id">
                {{ c.subjectName }} — {{ c.teacherName }} ({{ lessonTypeMeta[c.lessonType].label }})
              </option>
            </select>
            <p v-if="curriculumOptions.length === 0" class="ep-hint">
              Нет учебных планов для выбранного объекта/семестра.
            </p>
          </div>

          <div class="ep-field">
            <label>Аудитория</label>
            <select class="select-dropdown ep-select" v-model="editRoomId">
              <option value="">Выберите аудиторию…</option>
              <option v-for="r in allRooms" :key="r.id" :value="r.id">
                {{ r.name }} · {{ r.buildingName }} ({{ r.capacity }} мест)
              </option>
            </select>
          </div>
        </div>

        <footer class="ep-footer">
          <button class="ep-cancel" @click="closePanel">Отмена</button>
          <button class="ep-save" :disabled="!editFormValid || saving" @click="savePair">
            <Save :size="16" /> {{ saving ? 'Сохранение…' : 'Сохранить' }}
          </button>
        </footer>
      </aside>
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
.top-bar { display: flex; justify-content: space-between; align-items: center; padding: 16px 24px; border-bottom: 1px solid #f1f5f9; flex-wrap: wrap; gap: 12px; }
.left-controls, .right-controls { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.label { font-size: 14px; color: #64748b; }

.badge { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 500; }
.badge-green { color: #059669; background-color: transparent; border: none; padding: 6px 0; }

/* Бейдж типа недели (красная/синяя) — как в макете */
.week-badge { display: inline-flex; align-items: center; gap: 6px; padding: 6px 12px; border-radius: 9999px; font-size: 13px; font-weight: 700; border: 1px solid transparent; }
.week-red { background-color: #fef2f2; color: #b91c1c; border-color: #fecaca; }
.week-blue { background-color: #eff6ff; color: #1d4ed8; border-color: #bfdbfe; }

/* Индикаторная полоса типа недели над сеткой */
.week-indicator { display: flex; align-items: center; gap: 8px; padding: 8px 14px; border-radius: 8px; font-size: 13px; font-weight: 500; margin-bottom: 16px; border: 1px solid transparent; }
.week-indicator.week-red { background-color: #fef2f2; color: #b91c1c; border-color: #fee2e2; }
.week-indicator.week-blue { background-color: #eff6ff; color: #1d4ed8; border-color: #dbeafe; }

.entity-tabs { display: flex; gap: 12px; padding: 20px 24px 0 24px; }
.entity-btn { display: flex; align-items: center; gap: 8px; padding: 10px 24px; border-radius: 12px; border: 1px solid #cbd5e1; background: white; color: #334155; font-size: 14px; font-weight: 500; cursor: pointer; transition: all 0.2s; }
.entity-btn:hover:not(.active) { background-color: #f8fafc; }
.entity-btn.active { background-color: #1a4d9c; color: white; border-color: #1a4d9c; }

.filters-bar { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; }
.filters-left { display: flex; gap: 12px; flex-wrap: wrap; }
.select-dropdown { padding: 10px 36px 10px 16px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: white; color: #334155; font-size: 14px; outline: none; appearance: none; background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%2394a3b8' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E"); background-repeat: no-repeat; background-position: right 12px center; min-width: 180px; cursor: pointer; }
.select-dropdown:hover:not(:disabled) { border-color: #94a3b8; }
.week-select { min-width: 130px; font-weight: 500; color: #0f172a; }
.sem-select { min-width: 200px; font-weight: 500; color: #0f172a; }
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
.time-cell { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 12px 4px; min-height: 96px; }
.slot-number { color: #1a4d9c; font-weight: 700; font-size: 16px; margin-bottom: 4px; }
.slot-time { color: #94a3b8; font-size: 11px; line-height: 1.2; text-align: center; }
.empty-cell { background-color: white; border: 1px solid #e2e8f0; border-radius: 8px; transition: background-color 0.2s; min-height: 96px; }
.empty-cell:hover { background-color: #f8fafc; cursor: pointer; }

/* Карточка занятия в сетке */
.lesson-cell { border: 1px solid #e2e8f0; border-left-width: 4px; border-radius: 8px; padding: 8px 10px; min-height: 96px; display: flex; flex-direction: column; gap: 2px; overflow: hidden; cursor: pointer; transition: box-shadow 0.2s; }
.lesson-cell:hover { box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
.lc-subject { font-weight: 700; font-size: 12px; line-height: 1.25; color: #0f172a; }
.lc-line { font-size: 11px; color: #475569; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.lc-type { font-size: 10px; color: #94a3b8; margin-top: auto; }

.lt-lecture { background-color: #eff6ff; border-left-color: #3b82f6; }
.lt-practice { background-color: #f0fdf4; border-left-color: #22c55e; }
.lt-lab { background-color: #faf5ff; border-left-color: #a855f7; }
.lt-exam { background-color: #fef2f2; border-left-color: #ef4444; }
.lt-consult { background-color: #fefce8; border-left-color: #eab308; }
.lt-default { background-color: #f8fafc; border-left-color: #94a3b8; }

/* Тулбар выбора пары */
.toolbar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.tool-btn { display: inline-flex; align-items: center; gap: 6px; padding: 9px 14px; border-radius: 8px; border: 1px solid #cbd5e1; background: white; color: #475569; font-size: 13px; font-weight: 500; cursor: pointer; transition: all 0.2s; }
.tool-btn:hover { background: #f8fafc; }
.tool-danger { color: #dc2626; border-color: #fecaca; }
.tool-danger:hover { background: #fef2f2; }
.tool-info { color: #2563eb; border-color: #bfdbfe; }
.tool-info:hover { background: #eff6ff; }
.move-hint { display: inline-flex; align-items: center; gap: 6px; padding: 8px 12px; border-radius: 8px; background: #eff6ff; color: #1d4ed8; font-size: 13px; font-weight: 500; }

/* Сетка + панель */
.grid-and-panel { display: flex; gap: 16px; align-items: flex-start; }
.grid-and-panel .schedule-table { flex: 1; min-width: 0; }

/* Состояния ячеек */
.lesson-cell.cell-selected { box-shadow: 0 0 0 2px #1a4d9c; }
.lesson-cell.cell-dim { opacity: 0.55; }
.empty-cell { display: flex; align-items: center; justify-content: center; }
.empty-cell.cell-movable { background: #f0fdf4; border-color: #86efac; }
.cell-plus { opacity: 0.8; }

/* Панель добавления пары */
.edit-panel { width: 340px; flex-shrink: 0; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; background: white; box-shadow: 0 4px 12px rgba(0,0,0,0.05); }
.ep-header { display: flex; align-items: center; justify-content: space-between; padding: 14px 16px; background: #1a4d9c; color: white; }
.ep-eyebrow { margin: 0; font-size: 11px; opacity: 0.75; }
.ep-title { margin: 2px 0 0 0; font-size: 15px; font-weight: 600; }
.ep-close { background: rgba(255,255,255,0.15); border: none; color: white; width: 28px; height: 28px; border-radius: 8px; display: flex; align-items: center; justify-content: center; cursor: pointer; }
.ep-close:hover { background: rgba(255,255,255,0.3); }
.ep-body { padding: 16px; display: flex; flex-direction: column; gap: 16px; }
.ep-field { display: flex; flex-direction: column; gap: 6px; }
.ep-field label { font-size: 11px; font-weight: 700; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.03em; }
.ep-grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.ep-grid2 > div { display: flex; flex-direction: column; gap: 6px; }
.ep-select { width: 100%; min-width: 0; box-sizing: border-box; }
.ep-hint { margin: 0; font-size: 12px; color: #94a3b8; }
.ep-warn { font-size: 12px; color: #b45309; background: #fffbeb; border: 1px solid #fde68a; border-radius: 8px; padding: 8px 10px; }
.ep-footer { display: flex; gap: 10px; padding: 14px 16px; border-top: 1px solid #f1f5f9; background: #f8fafc; }
.ep-cancel { flex: 1; padding: 10px; border-radius: 8px; border: none; background: #e2e8f0; color: #475569; font-size: 14px; cursor: pointer; }
.ep-cancel:hover { background: #cbd5e1; }
.ep-save { flex: 1; display: inline-flex; align-items: center; justify-content: center; gap: 6px; padding: 10px; border-radius: 8px; border: none; background: #1a4d9c; color: white; font-size: 14px; font-weight: 600; cursor: pointer; }
.ep-save:hover:not(:disabled) { background: #143c82; }
.ep-save:disabled { background: #cbd5e1; color: #94a3b8; cursor: not-allowed; }

.dropdown-wrapper { position: relative; }
.dropdown-menu { position: absolute; top: calc(100% + 8px); right: 0; background: white; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05); padding: 8px; min-width: 270px; z-index: 50; display: flex; flex-direction: column; gap: 4px; }
.dropdown-item { display: flex; align-items: center; gap: 12px; padding: 10px 14px; border-radius: 8px; cursor: pointer; color: #1e293b; font-size: 14px; font-weight: 500; transition: all 0.2s ease; }
.dropdown-item .dd-icon { color: #475569; transition: color 0.2s ease; }
.dropdown-item:hover { background-color: #f4f6f8; color: #1a4d9c; }
.dropdown-item:hover .dd-icon { color: #1a4d9c; }
</style>
