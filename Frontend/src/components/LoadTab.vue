<script setup lang="ts">
import { ref, computed } from 'vue'
import { Search, BarChart2, FileText, Users, BookOpen } from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'

// Состояние фильтров
const selectedInstitute = ref('')
const selectedDept = ref('')
const selectedTeacher = ref('')
const searchQuery = ref('')

const isInstituteSelected = computed(() => selectedInstitute.value !== '')

// Списки для фильтров
const institutes = ['ММИГ', 'ИТКН', 'ИНЭУ']
const departments = ['Кафедра высшей математики', 'Кафедра АСУ', 'Кафедра физики']
const teachers = ['Иванов Иван Иванович', 'Петрова Мария Сергеевна', 'Сидоров Петр Александрович']

// Данные для таблицы (как на твоем скриншоте)
const loadData = [
  { id: 1, teacher: 'Иванов Иван Иванович', subject: 'Математический анализ', group: 'ИВТ-301', type: 'ЛЕК', total: 36, hours: 2 },
  { id: 2, teacher: 'Иванов Иван Иванович', subject: 'Математический анализ', group: 'ИВТ-301', type: 'ПРАК', total: 36, hours: 2 },
  { id: 3, teacher: 'Петрова Мария Сергеевна', subject: 'Программирование', group: 'ИВТ-301', type: 'ЛЕК', total: 36, hours: 2 },
  { id: 4, teacher: 'Сидоров Петр Александрович', subject: 'Базы данных', group: 'ПИ-201', type: 'ЛЕК', total: 36, hours: 2 },
]

// 18 недель
const weeks = Array.from({ length: 18 }, (_, i) => i + 1)
</script>

<template>
  <div class="load-container">
    
    <header class="load-header">
      <div class="title-group">
        <BarChart2 :size="24" color="#1a4d9c" />
        <h2>Нагрузка (ММИС)</h2>
      </div>
      <div class="total-hours-badge">
        <FileText :size="16" /> Всего часов: 144
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
        <select v-model="selectedDept" class="select-dropdown" :disabled="!isInstituteSelected">
          <option value="" disabled selected>Все кафедры</option>
          <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
        </select>
      </div>

      <div class="filter-group">
        <label>Преподаватель</label>
        <select v-model="selectedTeacher" class="select-dropdown" :disabled="!isInstituteSelected">
          <option value="" disabled selected>Все преподаватели</option>
          <option v-for="t in teachers" :key="t" :value="t">{{ t }}</option>
        </select>
      </div>

      <div class="filter-group search-group">
        <label>Поиск по предмету</label>
        <BaseInput v-model="searchQuery" placeholder="Название дисциплины...">
          <template #left-icon><Search :size="16" /></template>
        </BaseInput>
      </div>
    </div>

    <div class="table-wrapper">
      <table class="load-table">
        <thead>
          <tr>
            <th class="main-th">Преподаватель / Предмет</th>
            <th>Группа</th>
            <th>Тип</th>
            <th class="total-th">Всего</th>
            <th v-for="w in weeks" :key="w" class="week-th">{{ w }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in loadData" :key="row.id" class="data-row">
            
            <td class="main-td">
              <div class="teacher-name">{{ row.teacher }}</div>
              <div class="subject-name">
                <BookOpen :size="14" /> {{ row.subject }}
              </div>
            </td>
            
            <td>
              <div class="group-cell">
                <Users :size="14" color="#94a3b8" /> {{ row.group }}
              </div>
            </td>
            
            <td>
              <span class="type-badge" :class="row.type === 'ЛЕК' ? 'badge-lek' : 'badge-prak'">
                {{ row.type }}
              </span>
            </td>
            
            <td class="total-td"><b>{{ row.total }}</b></td>
            
            <td v-for="w in weeks" :key="w" class="week-td">
              {{ row.hours }}
            </td>

          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.load-container {
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  min-height: calc(100vh - 60px);
  font-family: sans-serif;
}

/* Шапка */
.load-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
}

.title-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

h2 { font-size: 20px; color: #1e293b; margin: 0; }

.total-hours-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #1e293b;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
}

/* Фильтры */
.filters-bar {
  display: flex;
  gap: 20px;
  padding: 0 24px 20px 24px;
  border-bottom: 1px solid #f1f5f9;
}

.filter-group { display: flex; flex-direction: column; gap: 8px; flex: 1; }
.search-group { flex: 1.5; }

label { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; }

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

.select-dropdown:disabled { background-color: #f8fafc; cursor: not-allowed; color: #94a3b8; }

/* Таблица */
.table-wrapper {
  overflow-x: auto;
  padding: 0 24px 24px 24px;
}

.load-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  margin-top: 16px;
}

.load-table th {
  color: #64748b;
  font-weight: 600;
  padding: 12px 8px;
  border-bottom: 1px solid #e2e8f0;
  text-align: center;
  font-size: 12px;
}

.main-th { text-align: left !important; min-width: 250px; }
.total-th { font-weight: 700; color: #1e293b; }
.week-th { min-width: 36px; }

/* Строки данных */
.data-row td {
  padding: 16px 8px;
  border-bottom: 1px solid #f1f5f9;
  text-align: center;
  vertical-align: middle;
}

.data-row .main-td { text-align: left; }

.teacher-name {
  font-weight: 500;
  color: #1e293b;
  font-size: 13px;
  margin-bottom: 4px;
}

.subject-name {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 12px;
}

.group-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: #475569;
}

.type-badge {
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 700;
}

.badge-lek { background-color: #eff6ff; color: #3b82f6; }
.badge-prak { background-color: #f0fdf4; color: #22c55e; }

.total-td { color: #1e293b; }
.week-td { color: #1a4d9c; font-weight: 500; }

.data-row:hover td { background-color: #f8fafc; }
</style>