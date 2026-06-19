<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  Settings2, Search, User, MapPin, Users, BookOpen, Database,
  ChevronDown, GraduationCap, Building2, Layers, Filter, Trash2, Plus, Wrench, Save, SlidersHorizontal
} from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'
import { lookups } from '../api/lookups'
import { management } from '../api/management'
import { ApiError } from '../api/http'
import type { RoomDto, EquipmentDto, ConstraintConfigDto, BuildingDto, ConstraintType } from '../api/types'

const searchQuery = ref('')

// Текущая активная вкладка ('teachers', 'rooms', 'groups', 'load', 'objects')
const activeTab = ref('objects')

// ==========================================
// ДАННЫЕ
// ==========================================
const teachersTreeData = ref([
  { id: 1, name: 'Институт информационных технологий', type: 'inst', isOpen: true, children: [
    { id: 11, name: 'Кафедра программирования', type: 'dept', badge: 2, isOpen: true, children: [
      { id: 111, name: 'Иванов Иван Иванович', desc: 'Кафедра программирования' },
      { id: 112, name: 'Петрова Мария Сергеевна', desc: 'Кафедра программирования' }
    ]}
  ]}
])

const roomsTreeData = ref([
  { id: 1, name: 'Главный корпус', type: 'building', badge: 4, isOpen: true, children: [
    { id: 101, name: 'Ауд. 305', desc: 'Главный корпус' },
    { id: 102, name: 'Ауд. 308', desc: 'Главный корпус' },
    { id: 103, name: 'Ауд. 401', desc: 'Главный корпус' },
    { id: 104, name: 'Ауд. Актовый зал', desc: 'Главный корпус' }
  ]}
])

const groupsTreeData = ref([
  { id: 1, name: 'Институт информационных технологий', type: 'inst', isOpen: true, children: [
    { id: 11, name: 'Бакалавриат', type: 'degree', badge: 4, isOpen: true, children: [
      { id: 111, name: '1 курс', type: 'course', badge: 1, isOpen: true, children: [{ id: 1111, name: 'КБ-101', desc: '1 курс · 32 чел.' }]},
      { id: 112, name: '2 курс', type: 'course', badge: 1, isOpen: true, children: [{ id: 1121, name: 'ПИ-201', desc: '2 курс · 25 чел.' }]}
    ]}
  ]}
])

const loadListData = ref([
  { id: 1, subject: 'Программирование', teacher: 'Иванов Иван Иванович', type: 'Практика', group: 'ИВТ-301', hours: 4, sub: 2 },
  { id: 2, subject: 'Веб-разработка', teacher: 'Петрова Мария Сергеевна', type: 'Лаб. работа', group: 'ИВТ-301', hours: 2, sub: 2 },
  { id: 3, subject: 'Математический анализ', teacher: 'Козлова Анна Владимировна', type: 'Лекция', group: 'ИВТ-301', hours: 4, sub: 0 },
])

// ==========================================
// ЛОГИКА ОБЪЕКТОВ (реальные данные с бэкенда)
// ==========================================
const activeObjectTab = ref<'audiences' | 'equipment' | 'weights'>('audiences')
const isCreatingRoom = ref(false)
const isCreatingEquipment = ref(false)
const objectsError = ref('')

const classrooms = ref<RoomDto[]>([])
const equipments = ref<EquipmentDto[]>([])
const buildings = ref<BuildingDto[]>([])
const constraints = ref<ConstraintConfigDto[]>([])

// Форма аудитории.
const roomForm = ref<{ id: string | null; name: string; buildingId: string; capacity: number }>(
  { id: null, name: '', buildingId: '', capacity: 30 })
const newEquipmentName = ref('')

// Человекочитаемые названия типов мягких ограничений.
const constraintLabels: Record<ConstraintType, string> = {
  TeacherGap: 'Окна в расписании преподавателя',
  StudentGap: 'Окна в расписании группы',
  ClassroomAvailability: 'Недоступная аудитория',
  TeacherAvailability: 'Недоступный слот преподавателя',
}

async function loadObjects() {
  objectsError.value = ''
  try {
    [classrooms.value, equipments.value, buildings.value, constraints.value] = await Promise.all([
      management.classrooms(), management.equipments(), lookups.buildings(), management.constraints(),
    ])
  } catch (e) {
    objectsError.value = e instanceof ApiError ? e.message : 'Не удалось загрузить данные.'
  }
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

onMounted(loadObjects)

// ==========================================
// ВСПОМОГАТЕЛЬНАЯ ЛОГИКА
// ==========================================
const listTitle = computed(() => {
  if (activeTab.value === 'teachers') return 'Преподаватели'
  if (activeTab.value === 'rooms') return 'Аудитории'
  if (activeTab.value === 'groups') return 'Группы'
  if (activeTab.value === 'load') return 'Нагрузка'
  return 'Объекты'
})

const listTotalCount = computed(() => {
  if (activeTab.value === 'teachers') return 5
  if (activeTab.value === 'rooms') return 5
  if (activeTab.value === 'groups') return 7
  if (activeTab.value === 'load') return '12/12'
  return 0
})

const toggleNode = (node: any) => { if (node.children) node.isOpen = !node.isOpen }
const getBadgeClass = (type: string) => {
  if (type === 'Практика') return 'badge-practice'
  if (type === 'Лаб. работа') return 'badge-lab'
  if (type === 'Лекция') return 'badge-lecture'
  return ''
}
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

      <div v-if="activeTab === 'load'" class="load-filters-block">
        <div class="filters-header"><Filter :size="14" /> Фильтры</div>
        <select class="select-dropdown"><option>Все преподаватели</option></select>
        <select class="select-dropdown"><option>Все группы</option></select>
        <select class="select-dropdown"><option>Все дисциплины</option></select>
      </div>

      <div v-if="activeTab === 'objects'" class="objects-summary">
        <Database :size="48" color="#cbd5e1" class="summary-icon" />
        <p class="summary-title">Управление объектами системы</p>
        <div class="summary-row"><span>Аудиторий:</span> <b>{{ classrooms.length }}</b></div>
        <div class="summary-row"><span>Типов оборудования:</span> <b>{{ equipments.length }}</b></div>
        <div class="summary-row"><span>Весов ограничений:</span> <b>{{ constraints.length }}</b></div>
      </div>

      <div v-if="activeTab !== 'objects'" class="list-header" :class="{ 'mt-4': activeTab === 'load' }">
        <span class="list-title">{{ listTitle }}</span>
        <span class="badge" :class="{ 'text-badge': activeTab === 'load' }">{{ listTotalCount }}</span>
      </div>

      <div v-if="activeTab === 'teachers'" class="tree-view">
        <div v-for="inst in teachersTreeData" :key="inst.id" class="tree-node">
          <div class="node-header" @click="toggleNode(inst)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !inst.isOpen }" />
            <GraduationCap :size="16" color="#10b981" />
            <span class="node-text">{{ inst.name }}</span>
          </div>
          <div v-show="inst.isOpen" class="node-children">
            <div v-for="dept in inst.children" :key="dept.id" class="tree-node">
              <div class="node-header" @click="toggleNode(dept)">
                <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !dept.isOpen }" />
                <Building2 :size="16" color="#64748b" />
                <span class="node-text">{{ dept.name }}</span>
              </div>
              <div v-show="dept.isOpen" class="node-children">
                <div v-for="teacher in dept.children" :key="teacher.id" class="leaf-node">
                  <div class="leaf-title">{{ teacher.name }}</div>
                  <div class="leaf-subtitle">{{ teacher.desc }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="activeTab === 'rooms'" class="tree-view">
        <div v-for="building in roomsTreeData" :key="building.id" class="tree-node">
          <div class="node-header" @click="toggleNode(building)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !building.isOpen }" />
            <Building2 :size="16" color="#a855f7" />
            <span class="node-text">{{ building.name }}</span>
          </div>
          <div v-show="building.isOpen" class="node-children">
            <div v-for="room in building.children" :key="room.id" class="leaf-node">
              <div class="leaf-title">{{ room.name }}</div>
              <div class="leaf-subtitle">{{ room.desc }}</div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="activeTab === 'groups'" class="tree-view">
        <div v-for="inst in groupsTreeData" :key="inst.id" class="tree-node">
          <div class="node-header" @click="toggleNode(inst)">
            <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !inst.isOpen }" />
            <GraduationCap :size="16" color="#10b981" />
            <span class="node-text">{{ inst.name }}</span>
          </div>
          <div v-show="inst.isOpen" class="node-children">
            <div v-for="degree in inst.children" :key="degree.id" class="tree-node">
              <div class="node-header" @click="toggleNode(degree)">
                <ChevronDown :size="16" color="#94a3b8" class="chevron-icon" :class="{ 'is-closed': !degree.isOpen }" />
                <Layers :size="16" color="#14b8a6" />
                <span class="node-text">{{ degree.name }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="activeTab === 'load'" class="load-card-list">
        <div v-for="item in loadListData" :key="item.id" class="load-card">
          <div class="load-title">{{ item.subject }}</div>
          <div class="load-subtitle">{{ item.teacher }}</div>
          <div class="load-meta">
            <span class="type-badge" :class="getBadgeClass(item.type)">{{ item.type }}</span>
            <span class="load-group">{{ item.group }} · {{ item.hours }}ч/н</span>
          </div>
        </div>
      </div>
    </aside>

    <main class="main-workspace" :class="{ 'is-objects-mode': activeTab === 'objects' }">
      
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
                <input
                  type="text"
                  v-model="newEquipmentName"
                  @keyup.enter="saveEquipment"
                  placeholder="Название оборудования..."
                  class="eq-input"
                />
                <button class="add-eq-btn" @click="saveEquipment">
                  <Plus :size="16" />
                </button>
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
              <div v-if="constraints.length === 0" class="muted-note">Веса не настроены.</div>
              <div class="weights-list">
                <div v-for="c in constraints" :key="c.id" class="weight-item">
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
                <div class="form-group">
                  <label>КОРПУС</label>
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
            
            <div v-if="isCreatingEquipment" class="room-form-wrapper">
              <header class="form-header">
                <div class="form-icon-box bg-blue-light"><Wrench :size="24" color="#3b82f6" /></div>
                <div class="form-title-group">
                  <h3>Новое оборудование</h3>
                  <p>Добавьте параметры нового типа оборудования</p>
                </div>
              </header>
              <div class="form-grid">
                <div class="form-group">
                  <label>НАЗВАНИЕ</label>
                  <input type="text" v-model="newEquipmentName" placeholder="например, 3D-принтер" class="custom-input" />
                </div>
                <div class="form-group">
                  <label>КАТЕГОРИЯ</label>
                  <select class="custom-select">
                    <option>Базовое</option>
                    <option>Мультимедиа</option>
                    <option>Специализированное</option>
                  </select>
                </div>
              </div>
              <button class="save-btn" @click="saveEquipment"><Save :size="16" /> Сохранить</button>
            </div>

            <div v-else class="empty-state-wrapper">
              <div class="empty-state">
                <div class="empty-icon-box bg-blue-light"><Wrench :size="40" color="#3b82f6" /></div>
                <h3>Справочник оборудования</h3>
                <p>Добавляйте новые типы оборудования. Они<br>будут доступны при настройке аудиторий и<br>ограничений нагрузки.</p>
                <span class="info-text">{{ equipments.length }} типов оборудования в системе</span>
              </div>
            </div>
          </template>

        </div>
      </template>

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
.list-header.mt-4 { margin-top: 16px; }
.list-title { font-size: 13px; font-weight: 600; color: #64748b; }
.badge { background-color: #f1f5f9; color: #64748b; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.badge.small { padding: 2px 6px; font-size: 10px; }
.text-badge { background-color: #f8fafc; font-weight: 500; }

.tree-view { display: flex; flex-direction: column; gap: 8px; padding-right: 8px; }
.tree-node { display: flex; flex-direction: column; }
.node-header { display: flex; align-items: center; gap: 8px; padding: 6px 0; cursor: pointer; user-select: none; }
.chevron-icon { transition: transform 0.2s ease; }
.chevron-icon.is-closed { transform: rotate(-90deg); }
.node-text { font-size: 13px; font-weight: 500; color: #334155; white-space: nowrap; }
.node-children { padding-left: 20px; display: flex; flex-direction: column; gap: 4px; border-left: 2px solid #f1f5f9; margin-left: 7px; margin-top: 4px; }
.leaf-node { padding: 8px 12px; margin-left: 8px; border-radius: 8px; cursor: pointer; transition: background-color 0.2s; }
.leaf-node:hover { background-color: #f8fafc; }
.leaf-title { font-size: 13px; font-weight: 500; color: #1e293b; margin-bottom: 2px; white-space: nowrap; }
.leaf-subtitle { font-size: 11px; color: #94a3b8; white-space: nowrap; }

.load-filters-block { display: flex; flex-direction: column; gap: 10px; margin-bottom: 20px; }
.filters-header { display: flex; align-items: center; gap: 6px; font-size: 12px; color: #64748b; font-weight: 500; margin-bottom: 4px; }
.select-dropdown { height: 36px; padding: 0 10px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: white; font-size: 13px; outline: none; cursor: pointer; color: #1e293b; }
.load-card-list { display: flex; flex-direction: column; gap: 16px; padding-right: 8px; }
.load-card { display: flex; flex-direction: column; gap: 6px; padding: 12px 0; border-bottom: 1px solid #f1f5f9; }
.load-card:last-child { border-bottom: none; }
.load-title { font-size: 14px; font-weight: 600; color: #1e293b; }
.load-subtitle { font-size: 12px; color: #94a3b8; }
.load-meta { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; margin-top: 4px; }
.type-badge { padding: 4px 8px; border-radius: 12px; font-size: 11px; font-weight: 500; }
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
.empty-icon-box { width: 70px; height: 70px; border-radius: 16px; display: flex; justify-content: center; align-items: center; margin-bottom: 20px; background-color: #e2e8f0;}
.empty-state h3 { font-size: 18px; color: #1e293b; margin: 0 0 12px 0; font-weight: 600; }
.empty-state p { font-size: 14px; color: #64748b; line-height: 1.5; margin: 0 0 16px 0; }
.info-text { font-size: 12px; color: #94a3b8; }
.bg-purple-light { background-color: #f3e8ff; }
.bg-blue-light { background-color: #e0f2fe; }

/* Центральная колонка (Списки объектов) */
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
.management-item { display: flex; justify-content: space-between; align-items: flex-start; padding: 12px 16px; border: 1px solid #f1f5f9; border-radius: 12px; transition: border-color 0.2s; }
.management-item:hover { border-color: #cbd5e1; }
.m-title { font-size: 13px; font-weight: 600; color: #1e293b; margin-bottom: 4px; }
.m-desc { font-size: 11px; color: #94a3b8; }
.delete-btn { background: none; border: none; color: #cbd5e1; cursor: pointer; padding: 4px; border-radius: 8px; transition: all 0.2s; margin-top: -2px; }
.delete-btn:hover { color: #dc2626; background-color: #fef2f2; }

/* Форма добавления оборудования */
.add-equipment-form { display: flex; gap: 8px; margin-bottom: 24px; }
.eq-input { flex-grow: 1; border: 1px solid #e2e8f0; border-radius: 8px; padding: 0 12px; font-size: 13px; outline: none; transition: border-color 0.2s; }
.eq-input:focus { border-color: #1d4ed8; }

.add-eq-btn { background-color: #1d4ed8; color: white; border: none; border-radius: 8px; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; cursor: pointer; flex-shrink: 0; transition: background-color 0.2s; }
.add-eq-btn:hover { background-color: #1e40af; }

.section-label { font-size: 10px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 12px; letter-spacing: 0.05em; }
.equipment-list { display: flex; flex-direction: column; gap: 4px; }
.equipment-item { display: flex; align-items: center; gap: 12px; padding: 8px 12px; font-size: 13px; color: #475569; cursor: pointer; border-radius: 8px; transition: background-color 0.2s; }
.equipment-item:hover { background-color: #f8fafc; color: #1e293b; }

/* Самая правая колонка */
.objects-main-content { flex-grow: 1; background-color: #fafbfc; display: flex; flex-direction: column; overflow-y: auto; }
.empty-state-wrapper { flex-grow: 1; display: flex; align-items: center; justify-content: center; padding: 40px; }

/* === ФОРМА === */
.room-form-wrapper { padding: 40px; width: 100%; max-width: 800px; }
.form-header { display: flex; align-items: center; gap: 16px; margin-bottom: 32px; }
.form-icon-box { width: 48px; height: 48px; border-radius: 12px; display: flex; justify-content: center; align-items: center; }
.form-title-group h3 { font-size: 18px; font-weight: 600; color: #1e293b; margin: 0 0 4px 0; }
.form-title-group p { font-size: 13px; color: #64748b; margin: 0; }

.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px; }
.form-group label { display: block; font-size: 11px; font-weight: 700; color: #94a3b8; text-transform: uppercase; margin-bottom: 8px; letter-spacing: 0.05em; }
.custom-input, .custom-select { width: 100%; height: 44px; border: 1px solid #cbd5e1; border-radius: 8px; padding: 0 14px; font-size: 14px; color: #1e293b; outline: none; background-color: white; transition: border-color 0.2s; }
.custom-input:focus, .custom-select:focus { border-color: #1d4ed8; }
.custom-input::placeholder { color: #94a3b8; }
.form-group.full-width { grid-column: span 2; }

.save-btn { display: flex; align-items: center; gap: 8px; background-color: #1d4ed8; color: white; padding: 12px 24px; border: none; border-radius: 8px; cursor: pointer; margin-top: 12px; font-weight: 500; font-size: 14px; transition: background-color 0.2s; }
.save-btn:hover { background-color: #1e40af; }

.form-actions { display: flex; gap: 12px; margin-top: 12px; }
.cancel-btn { background: #e2e8f0; color: #475569; padding: 12px 24px; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 14px; margin-top: 12px; }
.cancel-btn:hover { background: #cbd5e1; }

.objects-error { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; border-radius: 8px; padding: 10px 12px; font-size: 13px; margin-bottom: 16px; }
.muted-note { color: #94a3b8; font-size: 13px; padding: 8px 0; }
.eq-name { flex-grow: 1; }
.eq-del { margin-left: auto; }

.weights-list { display: flex; flex-direction: column; gap: 12px; }
.weight-item { display: flex; flex-direction: column; gap: 8px; padding: 12px; border: 1px solid #f1f5f9; border-radius: 12px; }
.w-label { font-size: 13px; font-weight: 600; color: #1e293b; }
.w-controls { display: flex; gap: 8px; }
.w-input { flex-grow: 1; height: 36px; border: 1px solid #cbd5e1; border-radius: 8px; padding: 0 12px; font-size: 14px; outline: none; }
.w-input:focus { border-color: #1d4ed8; }
.w-save { width: 40px; background: #1d4ed8; color: white; border: none; border-radius: 8px; display: flex; align-items: center; justify-content: center; cursor: pointer; flex-shrink: 0; }
.w-save:hover { background: #1e40af; }
</style>