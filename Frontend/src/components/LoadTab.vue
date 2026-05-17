<script setup lang="ts">
import { ref, computed } from 'vue'
import { Search, Info } from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'

const selectedInstitute = ref('')
const selectedDept = ref('')
const selectedTeacher = ref('')
const searchQuery = ref('')

const isInstituteSelected = computed(() => selectedInstitute.value !== '')

//потом поменять
const institutes = ['ИМИТ', 'ИГН', 'ММИС']
const departments = ['Кафедра высшей математики', 'Кафедра АСУ', 'Кафедра физики']
const teachers = ['Иванов Иван Иванович', 'Петров Петр Петрович', 'Сидоров Сидор Александрович']

const weeks = Array.from({ length: 18 }, (_, i) => i + 1)
</script>

<template>
  <div class="load-container">
    <header class="load-header">
      <div class="title-group">
        <h2>Нагрузка <span v-if="selectedInstitute">({{ selectedInstitute }})</span></h2>
      </div>
      <div class="total-hours-badge">
        Всего часов: <span>144</span>
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

      <div class="filter-group search-group">
        <label>Поиск по предмету</label>
        <BaseInput v-model="searchQuery" placeholder="Название доп. дисциплины...">
          <template #left-icon><Search :size="16" /></template>
        </BaseInput>
      </div>
    </div>

    <div class="table-wrapper">
      <table class="load-table">
        <thead>
          <tr>
            <th class="sticky-col main-th">Преподаватель / Предмет</th>
            <th>Группа</th>
            <th>Тип</th>
            <th class="total-th">Всего</th>
            <th v-for="w in weeks" :key="w" class="week-th">{{ w }}</th>
          </tr>
        </thead>
        <tbody>
          <tr class="placeholder-row">
            <td colspan="22">
              <div class="empty-table-msg">
                <Info :size="48" color="#94a3b8" />
                <p>Выберите институт и преподавателя для отображения нагрузки</p>
              </div>
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

.load-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid #f1f5f9;
}

h2 { font-size: 20px; color: #1e293b; margin: 0; }
h2 span { color: #64748b; font-weight: 400; }

.total-hours-badge {
  background-color: #eff6ff;
  color: #1d4ed8;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
}
.total-hours-badge span { font-weight: 700; }

.filters-bar {
  display: flex;
  gap: 20px;
  padding: 20px 24px;
  background-color: #fcfdfe;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

.search-group { flex: 1.5; }

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

/* Таблица */
.table-wrapper {
  overflow-x: auto; /* Горизонтальная прокрутка */
  padding: 0 24px 24px 24px;
}

.load-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 13px;
}

.load-table th {
  background-color: #f8fafc;
  color: #64748b;
  font-weight: 600;
  padding: 12px;
  border-bottom: 2px solid #e2e8f0;
  text-align: center;
}

.main-th { text-align: left !important; min-width: 250px; }
.total-th { background-color: #f1f5f9 !important; min-width: 60px; }
.week-th { min-width: 40px; font-size: 11px; }

/* Сообщение о пустой таблице */
.empty-table-msg {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 100px 0;
  color: #94a3b8;
}
.empty-table-msg p { margin-top: 16px; font-size: 16px; }
</style>