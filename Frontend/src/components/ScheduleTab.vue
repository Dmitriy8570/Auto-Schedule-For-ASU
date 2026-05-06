<script setup lang="ts">
import { ref } from 'vue'
import { 
  Calendar, RotateCcw, Sparkles, ChevronDown, Download, 
  User, Users, MapPin, Plus, CheckCircle2, Moon 
} from 'lucide-vue-next'

// Импортируем нашу умную кнопку!
import BaseButton from './BaseButton.vue'

const currentEntity = ref('teachers')
const selectedWeek = ref<number | string>('')
const totalWeeks = 18
const isTeacherSelected = ref(true)

const daysOfWeek = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб']

const timeSlots = [
  { id: 1, start: '08:00', end: '09:30' },
  { id: 2, start: '09:40', end: '11:10' },
  { id: 3, start: '11:20', end: '12:50' },
  { id: 4, start: '13:20', end: '14:50' },
  { id: 5, start: '15:00', end: '16:30' },
  { id: 6, start: '16:40', end: '18:10' },
  { id: 7, start: '18:20', end: '19:50' },
  { id: 8, start: '20:00', end: '21:30' },
]
</script>

<template>
  <div class="schedule-container">
    
    <!-- 1. ВЕРХНЯЯ ПАНЕЛЬ -->
    <header class="top-bar">
      <div class="left-controls">
        <Calendar :size="18" color="#64748b" />
        <span class="label">Неделя:</span>
        <select 
          class="select-dropdown week-select" 
          :class="{ 'is-placeholder': selectedWeek === '' }"
          v-model="selectedWeek"
        >
          <option value="" disabled hidden>Выберите неделю</option>
          <option v-for="week in totalWeeks" :key="week" :value="week">
            {{ week }} неделя
          </option>
        </select>
        
        <div class="badge badge-red">
          <Moon :size="14" /> Красная неделя
        </div>
        <div class="badge badge-green">
          <CheckCircle2 :size="14" /> Текущая
        </div>
      </div>

      <div class="right-controls">
        <BaseButton variant="outline">
          <RotateCcw :size="16" /> Сбросить до выгруженного
        </BaseButton>
        <BaseButton variant="gradient">
          <Sparkles :size="16" /> Автогенерация <ChevronDown :size="16" />
        </BaseButton>
        <BaseButton variant="outline">
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
          <select class="select-dropdown"><option>Все институты</option></select>
          <select class="select-dropdown disabled" disabled><option>Все кафедры</option></select>
          <select class="select-dropdown"><option>Выберите преподавателя</option></select>
        </template>
        <template v-if="currentEntity === 'groups'">
          <select class="select-dropdown"><option>Все институты</option></select>
          <select class="select-dropdown"><option>Все уровни</option></select>
          <select class="select-dropdown"><option>Все курсы</option></select>
          <select class="select-dropdown"><option>Выберите группу</option></select>
        </template>
        <template v-if="currentEntity === 'rooms'">
          <select class="select-dropdown"><option>Все корпусы</option></select>
          <select class="select-dropdown disabled" disabled><option>Выберите аудиторию...</option></select>
        </template>
      </div>
      
      <!-- Кнопка стала синей и активной -->
      <BaseButton variant="primary">
        <Plus :size="16" /> Добавить пару
      </BaseButton>
    </div>

    <!-- ========================================== -->
    <!-- 4. СЕТКА РАСПИСАНИЯ ИЛИ ПУСТОЕ СОСТОЯНИЕ -->
    <!-- ========================================== -->
    
    <!-- ЕСЛИ ПРЕПОДАВАТЕЛЬ ВЫБРАН (показываем таблицу) -->
    <div v-if="isTeacherSelected" class="schedule-grid-container">
      
      <!-- Красная плашка (Alert) -->
      <div class="alert-banner">
        <Moon :size="16" color="#dc2626" />
        <span>Красная неделя • Неделя 5 из 18</span>
      </div>

      <!-- Сама таблица -->
      <div class="schedule-table">
        
        <!-- Шапка таблицы (Дни недели) -->
        <div class="grid-row header-row">
          <div class="time-col-header">Пара</div>
          <div v-for="day in daysOfWeek" :key="day" class="day-header">
            {{ day }}
          </div>
        </div>

        <!-- Строки с парами -->
        <div v-for="slot in timeSlots" :key="slot.id" class="grid-row">
          <!-- Левая ячейка со временем -->
          <div class="time-cell">
            <span class="slot-number">{{ slot.id }}</span>
            <span class="slot-time">{{ slot.start }}<br>—<br>{{ slot.end }}</span>
          </div>
          <!-- Пустые ячейки для предметов -->
          <div v-for="day in daysOfWeek" :key="day" class="empty-cell"></div>
          
        </div>

      </div>
    </div>

    <!-- ИНАЧЕ, ЕСЛИ НИКТО НЕ ВЫБРАН (показываем человечка) -->
    <div v-else class="empty-state">
      <div class="empty-icon-wrapper">
        <User v-if="currentEntity === 'teachers'" :size="40" color="#94a3b8" />
        <Users v-else-if="currentEntity === 'groups'" :size="40" color="#94a3b8" />
        <MapPin v-else-if="currentEntity === 'rooms'" :size="40" color="#94a3b8" />
      </div>
      <h3>
        <span v-if="currentEntity === 'teachers'">Выберите преподавателя</span>
        <span v-else-if="currentEntity === 'groups'">Выберите группу</span>
        <span v-else-if="currentEntity === 'rooms'">Выберите аудиторию</span>
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
.badge-red { color: #dc2626; background-color: #fef2f2; border: 1px solid #fca5a5; padding: 6px 16px; border-radius: 100px; }
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
.select-dropdown.is-placeholder {color: #94a3b8;}
.select-dropdown option {color: #334155; }
.select-dropdown:disabled { background-color: #f8fafc; color: #94a3b8; border-color: #e2e8f0; cursor: not-allowed; opacity: 0.7; }

.empty-state { flex-grow: 1; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; }
.empty-icon-wrapper { background-color: #f1f5f9; padding: 24px; border-radius: 16px; margin-bottom: 24px; }
.empty-state h3 { margin: 0 0 8px 0; color: #334155; font-size: 18px; }
.empty-state p { margin: 0; color: #94a3b8; font-size: 14px; line-height: 1.5; }

/* --- СЕТКА РАСПИСАНИЯ --- */
.schedule-grid-container {
  padding: 0 24px 24px 24px;
  flex-grow: 1;
  display: flex;
  flex-direction: column;
}

/* Красная плашка */
.alert-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  background-color: #fef2f2; /* Нежно-розовый фон */
  color: #dc2626; /* Красный текст */
  padding: 12px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 20px;
}

/* Настройки таблицы */
.schedule-table {
  display: flex;
  flex-direction: column;
  gap: 12px; /* Расстояние между строками */
}

/* ГЛАВНАЯ МАГИЯ CSS GRID: Настраиваем колонки */
/* 1 колонка 60px, остальные 6 занимают равное свободное место (1fr) */
.grid-row {
  display: grid;
  grid-template-columns: 60px repeat(6, 1fr);
  gap: 12px; /* Расстояние между ячейками */
}

/* Шапка с днями недели */
.time-col-header {
  font-size: 13px;
  color: #94a3b8;
  display: flex;
  align-items: center;
  justify-content: center;
}

.day-header {
  background-color: #f1f5f9; /* Светло-голубоватый фон, как на макете */
  color: #1a4d9c; /* Синий текст */
  font-weight: 600;
  font-size: 14px;
  padding: 12px;
  border-radius: 8px;
  text-align: center;
}

/* Ячейки со временем (слева) */
.time-cell {
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 12px 4px;
  min-height: 80px; /* Задаем высоту строки */
}

.slot-number {
  color: #1a4d9c;
  font-weight: 700;
  font-size: 16px;
  margin-bottom: 4px;
}

.slot-time {
  color: #94a3b8;
  font-size: 11px;
  line-height: 1.2;
  text-align: center;
}

/* Пустые слоты (куда будем кидать предметы) */
.empty-cell {
  background-color: white;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  transition: background-color 0.2s;
}

.empty-cell:hover {
  background-color: #f8fafc; /* Легкая подсветка при наведении */
  cursor: pointer;
}
</style>