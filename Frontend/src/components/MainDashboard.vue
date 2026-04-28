<script setup lang="ts">
import { ref } from 'vue'
import TheSidebar from './TheSidebar.vue'
import ScheduleTab from './ScheduleTab.vue'

// Создаем переменную для хранения текущей вкладки.
const currentTab = ref('schedule')

// Перехватываем событие выхода
const emit = defineEmits(['logout'])
</script>

<template>
  <div class="dashboard-layout">
    
    <TheSidebar 
      :active-tab="currentTab" 
      @change-tab="(tabName) => currentTab = tabName"
      @logout="emit('logout')"
    />
    
    <div class="main-content">
      
      <ScheduleTab v-if="currentTab === 'schedule'" />

      <div v-if="currentTab === 'load'" class="page-content">
        <h1>Нагрузка</h1>
        <p>нагрузка преподавателей</p>
      </div>

      <div v-if="currentTab === 'history'" class="page-content">
        <h1>История изменений</h1>
        <p>кто что сделал</p>
      </div>

      <div v-if="currentTab === 'settings'" class="page-content">
        <h1>Ограничения</h1>
        <p>Настройки</p>
      </div>

    </div>
  </div>
</template>

<style scoped>
.dashboard-layout {
  display: flex;
  flex-direction: row; 
  min-height: 100vh;
  background-color: #f8fafc; 
}

.main-content {
  flex: 1;
  padding: 30px;
  overflow-y: auto; 
}

/* Временные стили для заглушек контента */
.page-content {
  background: white;
  padding: 40px;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
}

.page-content h1 {
  margin-top: 0;
  color: #1e293b;
}
</style>