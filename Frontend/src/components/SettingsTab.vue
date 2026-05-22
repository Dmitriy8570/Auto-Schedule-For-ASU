<script setup lang="ts">
import { ref, computed } from 'vue'
import { 
  Settings2, Search, User, MapPin, Users, BookOpen, Database, 
  ChevronDown, GraduationCap, Building2, Layers, Hash, Filter, Trash2, Plus, Wrench, Save
} from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'

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
// ЛОГИКА ОБЪЕКТОВ
// ==========================================
const activeObjectTab = ref('equipment') // Откроем сразу Оборудование
const isCreatingRoom = ref(false) 
const isCreatingEquipment = ref(false) // УПРАВЛЯЕТ ФОРМОЙ ОБОРУДОВАНИЯ

const objectsListData = ref([
  { id: 1, name: 'Ауд. 305', desc: 'Главный корпус - 30 мест' },
  { id: 2, name: 'Ауд. 308', desc: 'Главный корпус - 30 мест' },
  { id: 3, name: 'Ауд. 401', desc: 'Главный корпус - 100 мест' },
  { id: 4, name: 'Ауд. 215', desc: 'Технический корпус - 20 мест' },
  { id: 5, name: 'Ауд. Актовый зал', desc: 'Главный корпус - 500 мест' }
])

const equipmentListData = ref([
  'Проектор', 'Интерактивная доска', 'Компьютеры', 'Лабораторный стенд', 'Осциллограф', 
  '3D-принтер', 'Микроскоп', 'Графический планшет', 'Звуковое оборудование', 'Видеокамера', 
  'Химический вытяжной шкаф', 'Паяльная станция', 'Маркерная доска', 'Экран для проектора', 
  'Принтер', 'Сканер'
])

const newEquipmentName = ref('')

// Функция сохранения оборудования из правой формы
const saveEquipment = () => {
  const name = newEquipmentName.value.trim()
  if (name) {
    equipmentListData.value.unshift(name) // Добавляем в начало списка
  }
  newEquipmentName.value = '' // Очищаем поле
  isCreatingEquipment.value = false // Закрываем форму
}

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
        <div class="summary-row"><span>Аудиторий:</span> <b>5</b></div>
        <div class="summary-row"><span>Типов оборудования:</span> <b>{{ equipmentListData.length }}</b></div>
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
          </header>

          <div class="sub-sidebar-content">
            
            <template v-if="activeObjectTab === 'audiences'">
              <button class="create-btn" @click="isCreatingRoom = true"><Plus :size="16" /> Создать аудиторию</button>
              <div class="management-list">
                <div v-for="obj in objectsListData" :key="obj.id" class="management-item">
                  <div class="m-info">
                    <div class="m-title">{{ obj.name }}</div>
                    <div class="m-desc">{{ obj.desc }}</div>
                  </div>
                  <button class="delete-btn"><Trash2 :size="16" /></button>
                </div>
              </div>
            </template>

            <template v-if="activeObjectTab === 'equipment'">
              <div class="add-equipment-form">
                <input 
                  type="text" 
                  v-model="newEquipmentName" 
                  @keyup.enter="isCreatingEquipment = true" 
                  placeholder="Название оборудования..." 
                  class="eq-input" 
                />
                <button class="add-eq-btn" @click="isCreatingEquipment = true">
                  <Plus :size="16" />
                </button>
              </div>
              <div class="section-label">БАЗОВОЕ</div>
              <div class="equipment-list">
                <div v-for="(item, index) in equipmentListData" :key="index" class="equipment-item">
                  <Wrench :size="14" color="#94a3b8" /><span>{{ item }}</span>
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
                  <h3>Новая аудитория</h3>
                  <p>Заполните параметры аудитории</p>
                </div>
              </header>
              <div class="form-grid">
                <div class="form-group"><label>НОМЕР / НАЗВАНИЕ</label><input type="text" placeholder="например, 305" class="custom-input" /></div>
                <div class="form-group">
                  <label>КОРПУС</label>
                  <select class="custom-select"><option>Главный корпус</option><option>Технический корпус</option></select>
                </div>
                <div class="form-group"><label>ВМЕСТИМОСТЬ (МЕСТ)</label><input type="number" value="30" class="custom-input" /></div>
                <div class="form-group">
                  <label>ТИП АУДИТОРИИ</label>
                  <select class="custom-select"><option>Учебная аудитория</option><option>Компьютерный класс</option><option>Лаборатория</option></select>
                </div>
              </div>
              <div class="form-group full-width"><label>ОБОРУДОВАНИЕ</label><input type="text" placeholder="Добавить оборудование..." class="custom-input" /></div>
              <button class="save-btn" @click="isCreatingRoom = false"><Save :size="16" /> Сохранить</button>
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
                <span class="info-text">{{ equipmentListData.length }} типов оборудования в системе</span>
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
</style>