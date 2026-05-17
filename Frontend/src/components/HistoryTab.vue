<script setup lang="ts">
import { ref, computed } from 'vue'
import { 
  History, 
  RotateCw, 
  Trash2, 
  PlusCircle, 
  User, 
  Calendar, 
  Clock, 
  ChevronLeft, 
  ChevronRight 
} from 'lucide-vue-next'

// Состояние фильтров
const selectedInstitute = ref('')
const selectedDept = ref('')
const selectedTeacher = ref('')

// Проверка: выбран ли институт (чтобы разблокировать остальные фильтры)
const isInstituteSelected = computed(() => selectedInstitute.value !== '')

// Тестовые данные для списков (потом заменим на данные из БД)
const institutes = ['ММИГ', 'ИТКН', 'ИНЭУ']
const departments = ['Кафедра высшей математики', 'Кафедра АСУ', 'Кафедра физики']
const teachers = ['Иванов Иван Иванович', 'Петрова Мария Сергеевна', 'Сидоров Петр Александрович']

// Тестовые данные для истории изменений
const historyEvents = [
  { id: 1, type: 'add', title: 'Добавлено занятие', group: 'ИВТ-300', room: 'Ауд. 300', teacher: 'Иванов Иван Иванович', date: '17.05.2026', time: '15:51:55' },
  { id: 2, type: 'delete', title: 'Удалено занятие', group: 'ИВТ-301', room: 'Ауд. 301', teacher: 'Петрова Мария Сергеевна', date: '17.05.2026', time: '14:51:55' },
  { id: 3, type: 'edit', title: 'Изменено расписание', group: 'ИВТ-302', room: 'Ауд. 302', teacher: 'Сидоров Петр Александрович', date: '17.05.2026', time: '13:51:55' },
  { id: 4, type: 'add', title: 'Добавлено занятие', group: 'ИВТ-303', room: 'Ауд. 303', teacher: 'Козлова Анна Владимировна', date: '17.05.2026', time: '12:51:55' },
  { id: 5, type: 'delete', title: 'Удалено занятие', group: 'ИВТ-304', room: 'Ауд. 304', teacher: 'Смирнов Алексей Петрович', date: '17.05.2026', time: '11:51:55' },
  { id: 6, type: 'edit', title: 'Изменено расписание', group: 'ИВТ-300', room: 'Ауд. 305', teacher: 'Иванов Иван Иванович', date: '17.05.2026', time: '10:51:55' },
]

// Функция для выбора правильной иконки в зависимости от типа события
const getIcon = (type: string) => {
  if (type === 'add') return PlusCircle
  if (type === 'delete') return Trash2
  return RotateCw
}
</script>

<template>
  <div class="history-container">
    
    <header class="history-header">
      <div class="title-group">
        <History :size="24" color="#1e293b" />
        <h2>История изменений</h2>
      </div>
    </header>

    <div class="filters-bar">
      <div class="filter-group">
        <label>Институт</label>
        <select v-model="selectedInstitute" class="select-dropdown">
          <option value="" disabled selected>Все институты</option>
          <option v-for="inst in institutes" :key="inst" :value="inst">{{ inst }}</option>
        </select>
      </div>

      <div class="filter-group">
        <label>Кафедра</label>
        <select 
          v-model="selectedDept" 
          class="select-dropdown" 
          :disabled="!isInstituteSelected"
        >
          <option value="" disabled selected>Все кафедры</option>
          <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
        </select>
      </div>

      <div class="filter-group">
        <label>Преподаватель</label>
        <select 
          v-model="selectedTeacher" 
          class="select-dropdown" 
          :disabled="!isInstituteSelected"
        >
          <option value="" disabled selected>Все преподаватели</option>
          <option v-for="t in teachers" :key="t" :value="t">{{ t }}</option>
        </select>
      </div>
    </div>

    <div class="history-list-wrapper">
      <div class="history-list">
        
        <div v-for="event in historyEvents" :key="event.id" class="history-card">
          
          <div class="status-icon-wrapper" :class="`bg-${event.type}`">
            <component :is="getIcon(event.type)" :size="20" :class="`icon-${event.type}`" />
          </div>

          <div class="card-content">
            <div class="card-title">{{ event.title }}</div>
            <div class="card-subtitle">Группа {{ event.group }}, {{ event.room }}</div>
            
            <div class="card-meta">
              <div class="meta-item">
                <User :size="14" /> {{ event.teacher }}
              </div>
              <div class="meta-item">
                <Calendar :size="14" /> {{ event.date }}
              </div>
              <div class="meta-item">
                <Clock :size="14" /> {{ event.time }}
              </div>
            </div>
          </div>
          
        </div>

      </div>
    </div>

    <div class="pagination-bar">
      <div class="pagination-info">Показано 1-10 из 50</div>
      <div class="pagination-controls">
        <button class="page-btn"><ChevronLeft :size="18" /></button>
        <div class="page-text">1 / 5</div>
        <button class="page-btn"><ChevronRight :size="18" /></button>
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
  height: calc(100vh - 60px); /* Занимает всю высоту экрана минус шапка */
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

/* Список истории (со скроллом) */
.history-list-wrapper {
  flex-grow: 1;
  overflow-y: auto;
  padding: 24px;
  background-color: #f8fafc; /* Легкий серый фон за карточками */
}

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

/* Иконки статуса */
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

/* Контент карточки */
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
}

.card-meta {
  display: flex;
  gap: 16px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 13px;
}

/* Пагинация */
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

.page-btn:hover {
  background-color: #f8fafc;
  border-color: #94a3b8;
}

.page-text {
  font-size: 14px;
  font-weight: 500;
  color: #1e293b;
}
</style>