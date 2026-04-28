<script setup lang="ts">
import { ref } from 'vue'
import { 
  Calendar, RotateCcw, Sparkles, ChevronDown, Download, 
  User, Users, MapPin, Plus, CheckCircle2, Moon 
} from 'lucide-vue-next'

// Создаем переменную для отслеживания активной вкладки
// Возможные значения: 'teachers', 'groups', 'rooms'
const currentEntity = ref('teachers')
</script>

<template>
  <div class="schedule-container">
    
    <header class="top-bar">
      <div class="left-controls">
        <Calendar :size="18" color="#64748b" />
        <span class="label">Неделя:</span>
        <select class="select-dropdown week-select">
          <option>5 неделя ★</option>
        </select>
        
        <div class="badge badge-red">
          <Moon :size="14" /> Красная неделя
        </div>
        <div class="badge badge-green">
          <CheckCircle2 :size="14" /> Текущая
        </div>
      </div>

      <div class="right-controls">
        <button class="btn btn-outline">
          <RotateCcw :size="16" /> Сбросить до выгруженного
        </button>
        <button class="btn btn-purple">
          <Sparkles :size="16" /> Автогенерация <ChevronDown :size="16" />
        </button>
        <button class="btn btn-outline">
          <Download :size="16" /> Выгрузить
        </button>
      </div>
    </header>

    <div class="entity-tabs">
      <button 
        class="entity-btn" 
        :class="{ active: currentEntity === 'teachers' }"
        @click="currentEntity = 'teachers'"
      >
        <User :size="16" :color="currentEntity === 'teachers' ? 'white' : '#64748b'" /> Преподаватели
      </button>
      
      <button 
        class="entity-btn" 
        :class="{ active: currentEntity === 'groups' }"
        @click="currentEntity = 'groups'"
      >
        <Users :size="16" :color="currentEntity === 'groups' ? 'white' : '#64748b'" /> Группы
      </button>
      
      <button 
        class="entity-btn" 
        :class="{ active: currentEntity === 'rooms' }"
        @click="currentEntity = 'rooms'"
      >
        <MapPin :size="16" :color="currentEntity === 'rooms' ? 'white' : '#64748b'" /> Аудитории
      </button>
    </div>

    <div class="filters-bar">
      <div class="filters-left">
        
        <template v-if="currentEntity === 'teachers'">
          <select class="select-dropdown">
            <option>Все институты</option>
          </select>
          <select class="select-dropdown disabled" disabled>
            <option>Все кафедры</option>
          </select>
          <select class="select-dropdown">
            <option>Выберите преподавателя</option>
          </select>
        </template>

        <template v-if="currentEntity === 'groups'">
          <select class="select-dropdown">
            <option>Все институты</option>
          </select>
          <select class="select-dropdown">
            <option>Все уровни</option>
          </select>
          <select class="select-dropdown">
            <option>Все курсы</option>
          </select>
          <select class="select-dropdown">
            <option>Выберите группу</option>
          </select>
        </template>

        <template v-if="currentEntity === 'rooms'">
          <select class="select-dropdown">
            <option>Все корпусы</option>
          </select>
          <select class="select-dropdown disabled" disabled>
            <option>Выберите аудиторию...</option>
          </select>
        </template>

      </div>
      
      <button class="btn btn-light-grey disabled">
        <Plus :size="16" /> Добавить пару
      </button>
    </div>

    <div class="empty-state">
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
.schedule-container {
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  min-height: calc(100vh - 60px);
  font-family: sans-serif;
}

/* --- ВЕРХНЯЯ ПАНЕЛЬ --- */
.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  border-bottom: 1px solid #f1f5f9; /* Сделали линию светлее, как на макете */
}

.left-controls, .right-controls {
  display: flex;
  align-items: center;
  gap: 16px; /* Чуть увеличили расстояние между элементами */
}

.label {
  font-size: 14px;
  color: #64748b;
}

/* --- БЕЙДЖИ --- */
.badge {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 500;
}

.badge-red {
  color: #dc2626; /* Насыщенный красный */
  background-color: #fef2f2;
  border: 1px solid #fca5a5;
  padding: 6px 16px;
  border-radius: 100px; /* Идеальная форма таблетки */
}

.badge-green {
  color: #059669; /* Изумрудно-зеленый */
  background-color: transparent; /* Убрали фон */
  border: none; /* Убрали рамку */
  padding: 6px 0;
}

/* --- ПАНЕЛЬ СУЩНОСТЕЙ (Кнопки) --- */
.entity-tabs {
  display: flex;
  gap: 12px;
  padding: 20px 24px 0 24px;
}

.entity-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 24px; /* Сделали кнопки шире */
  border-radius: 12px; /* Увеличили скругление углов */
  border: 1px solid #cbd5e1; /* Мягкая серая рамка */
  background: white;
  color: #334155;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.entity-btn:hover:not(.active) {
  background-color: #f8fafc;
}

.entity-btn.active {
  background-color: #1a4d9c; /* Тот самый синий цвет */
  color: white;
  border-color: #1a4d9c;
}

/* --- ПАНЕЛЬ ФИЛЬТРОВ --- */
.filters-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
}

.filters-left {
  display: flex;
  gap: 12px;
}

/* --- ВЫПАДАЮЩИЕ СПИСКИ (Selects) --- */
.select-dropdown {
  padding: 10px 36px 10px 16px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background-color: white;
  color: #334155;
  font-size: 14px;
  outline: none;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%2394a3b8' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 12px center;
  min-width: 180px;
  cursor: pointer;
}

.select-dropdown:hover:not(:disabled) {
  border-color: #94a3b8;
}

.week-select {
  min-width: 130px;
  font-weight: 500;
  color: #0f172a;
}

.select-dropdown:disabled {
  background-color: #f8fafc;
  color: #94a3b8;
  border-color: #e2e8f0;
  cursor: not-allowed;
  opacity: 0.7; /* Делает текст и рамку полупрозрачными, как на дизайне */
}

/* --- КНОПКИ ДЕЙСТВИЙ --- */
.btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

/* Белые кнопки с контуром (Сбросить, Выгрузить) */
.btn-outline {
  background: white;
  border: 1px solid #cbd5e1;
  color: #475569;
}

.btn-outline:hover {
  background-color: #f8fafc;
}

/* Кнопка Автогенерации с красивым градиентом */
.btn-purple {
  /* Тот самый переход цвета слева (90deg) направо */
  background: linear-gradient(90deg, #9333ea 0%, #2563eb 100%);
  border: none;
  color: white;
}

.btn-purple:hover {
  /* При наведении делаем градиент чуть темнее */
  background: linear-gradient(90deg, #7e22ce 0%, #1d4ed8 100%);
}

/* Кнопка Добавить пару */
.btn-light-grey {
  background-color: #f8fafc; /* Очень светлый серо-голубой фон */
  color: #94a3b8; /* Светло-серый текст */
  border: 1px solid #f1f5f9; /* Едва заметная рамочка */
}

/* Если кнопка неактивна (как сейчас на макете) */
.btn.disabled {
  cursor: not-allowed;
  opacity: 0.8;
}

/* --- ПУСТОЕ СОСТОЯНИЕ (Центр) --- */
.empty-state {
  flex-grow: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  text-align: center;
}

.empty-icon-wrapper {
  background-color: #f1f5f9;
  padding: 24px;
  border-radius: 16px;
  margin-bottom: 24px;
}

.empty-state h3 {
  margin: 0 0 8px 0;
  color: #334155;
  font-size: 18px;
}

.empty-state p {
  margin: 0;
  color: #94a3b8;
  font-size: 14px;
  line-height: 1.5;
}
</style>