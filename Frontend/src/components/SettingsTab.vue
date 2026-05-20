<script setup lang="ts">
import { ref } from 'vue'
import { 
  Settings2, Search, User, MapPin, Users, BookOpen, Database, 
  ChevronDown, GraduationCap, Building2 
} from 'lucide-vue-next'
import BaseInput from './BaseInput.vue'

const searchQuery = ref('')
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
        <div class="obj-tab active">
          <User :size="20" />
          <span>Препод.</span>
        </div>
        <div class="obj-tab">
          <MapPin :size="20" />
          <span>Ауд.</span>
        </div>
        <div class="obj-tab">
          <Users :size="20" />
          <span>Группы</span>
        </div>
        <div class="obj-tab">
          <BookOpen :size="20" />
          <span>Нагрузка</span>
        </div>
        <div class="obj-tab">
          <Database :size="20" />
          <span>Объекты</span>
        </div>
      </div>

      <div class="list-header">
        <span class="list-title">Преподаватели</span>
        <span class="badge">5</span>
      </div>

      <div class="tree-view">
        
        <div class="tree-node">
          <div class="node-header">
            <ChevronDown :size="16" color="#94a3b8" />
            <GraduationCap :size="16" color="#3b82f6" />
            <span class="node-text">Институт информационных технологий</span>
          </div>
          
          <div class="node-children">
            <div class="tree-node">
              <div class="node-header">
                <ChevronDown :size="16" color="#94a3b8" />
                <Building2 :size="16" color="#64748b" />
                <span class="node-text">Кафедра программирования</span>
                <span class="badge small">2</span>
              </div>
              <div class="node-children">
                <div class="leaf-node">
                  <div class="leaf-title">Иванов Иван Иванович</div>
                  <div class="leaf-subtitle">Кафедра программирования</div>
                </div>
                <div class="leaf-node">
                  <div class="leaf-title">Петрова Мария Сергеевна</div>
                  <div class="leaf-subtitle">Кафедра программирования</div>
                </div>
              </div>
            </div>

            <div class="tree-node">
              <div class="node-header">
                <ChevronDown :size="16" color="#94a3b8" />
                <Building2 :size="16" color="#64748b" />
                <span class="node-text">Кафедра информационных систем</span>
              </div>
              <div class="node-children">
                <div class="leaf-node">
                  <div class="leaf-title">Сидоров Петр Александрович</div>
                  <div class="leaf-subtitle">Кафедра информационных систем</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="tree-node">
          <div class="node-header">
            <ChevronDown :size="16" color="#94a3b8" />
            <GraduationCap :size="16" color="#3b82f6" />
            <span class="node-text">Институт экономики и торговли</span>
            <span class="badge small">1</span>
          </div>
          <div class="node-children">
            <div class="tree-node">
              <div class="node-header">
                <ChevronDown :size="16" color="#94a3b8" />
                <Building2 :size="16" color="#64748b" />
                <span class="node-text">Кафедра экономики</span>
                <span class="badge small">1</span>
              </div>
              <div class="node-children">
                <div class="leaf-node">
                  <div class="leaf-title">Смирнов Алексей Петрович</div>
                  <div class="leaf-subtitle">Кафедра экономики</div>
                </div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </aside>

    <main class="main-workspace">
      <div class="empty-state">
        <div class="empty-icon-box">
          <Settings2 :size="40" color="#94a3b8" />
        </div>
        <h3>Выберите объект</h3>
        <p>Выберите преподавателя, аудиторию, группу или<br>запись нагрузки для настройки ограничений.</p>
        
        <div class="quick-actions">
          <div class="action-card card-blue">
            <div class="card-icon"><User :size="20" /></div>
            <div class="card-text">
              <div class="card-title">Преподаватели</div>
              <div class="card-subtitle">5 объектов</div>
            </div>
          </div>
          <div class="action-card card-purple">
            <div class="card-icon"><MapPin :size="20" /></div>
            <div class="card-text">
              <div class="card-title">Аудитории</div>
              <div class="card-subtitle">5 объектов</div>
            </div>
          </div>
          <div class="action-card card-green">
            <div class="card-icon"><Users :size="20" /></div>
            <div class="card-text">
              <div class="card-title">Группы</div>
              <div class="card-subtitle">7 объектов</div>
            </div>
          </div>
          <div class="action-card card-orange">
            <div class="card-icon"><BookOpen :size="20" /></div>
            <div class="card-text">
              <div class="card-title">Нагрузка</div>
              <div class="card-subtitle">12 объектов</div>
            </div>
          </div>
        </div>
      </div>
    </main>

  </div>
</template>

<style scoped>
.settings-container {
  display: flex;
  height: calc(100vh - 60px); 
  background-color: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  font-family: sans-serif;
}

/* ЛЕВАЯ ПАНЕЛЬ */
.sidebar-panel {
  display: flex;
  flex-direction: column;
  /* КЛЮЧЕВОЙ МОМЕНТ: width: max-content позволяет панели тянуться по длине текста,
     а flex-shrink: 0 не дает ей сжиматься. */
  width: max-content;
  min-width: 340px; 
  flex-shrink: 0;
  border-right: 1px solid #f1f5f9;
  padding: 20px;
  overflow-y: auto; /* Оставляем только вертикальный скролл */
}

.panel-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 20px;
}

.panel-header h2 {
  font-size: 18px;
  font-weight: 600;
  color: #1e293b;
  margin: 0;
}

.search-box {
  margin-bottom: 20px;
}

.object-tabs {
  display: flex;
  justify-content: space-between;
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.obj-tab {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 11px;
  font-weight: 500;
  cursor: pointer;
}

.obj-tab.active {
  color: #1a4d9c;
  position: relative;
}

.obj-tab.active::after {
  content: '';
  position: absolute;
  bottom: -13px;
  left: 0;
  right: 0;
  height: 2px;
  background-color: #1a4d9c;
  border-radius: 2px 2px 0 0;
}

.list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.list-title {
  font-size: 13px;
  font-weight: 600;
  color: #64748b;
}

.badge {
  background-color: #f1f5f9;
  color: #64748b;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.badge.small {
  padding: 2px 6px;
  font-size: 10px;
}

/* Дерево */
.tree-view {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.tree-node {
  display: flex;
  flex-direction: column;
}

.node-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 0;
  cursor: pointer;
}

.node-text {
  font-size: 13px;
  font-weight: 500;
  color: #334155;
  white-space: nowrap; /* Запрещаем перенос текста, чтобы панель раздвинулась */
}

.node-children {
  padding-left: 20px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  border-left: 1px solid #e2e8f0;
  margin-left: 7px;
  margin-top: 4px;
}

.leaf-node {
  padding: 8px 12px;
  margin-left: 8px;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.leaf-node:hover {
  background-color: #f8fafc;
}

.leaf-title {
  font-size: 13px;
  font-weight: 500;
  color: #1e293b;
  margin-bottom: 2px;
  white-space: nowrap;
}

.leaf-subtitle {
  font-size: 11px;
  color: #94a3b8;
  white-space: nowrap;
}

/* ПРАВАЯ ПАНЕЛЬ */
.main-workspace {
  flex-grow: 1;
  background-color: #fafbfc;
  display: flex;
  justify-content: center;
  align-items: center;
  overflow-y: auto;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.empty-icon-box {
  width: 70px;
  height: 70px;
  background-color: #e2e8f0;
  border-radius: 16px;
  display: flex;
  justify-content: center;
  align-items: center;
  margin-bottom: 20px;
}

.empty-state h3 {
  font-size: 18px;
  color: #1e293b;
  margin: 0 0 8px 0;
}

.empty-state p {
  font-size: 14px;
  color: #94a3b8;
  line-height: 1.5;
  margin: 0 0 32px 0;
}

.quick-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.action-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 20px;
  background-color: white;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  cursor: pointer;
  transition: box-shadow 0.2s, border-color 0.2s;
  text-align: left;
  min-width: 200px;
}

.action-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 4px;
}

.card-subtitle {
  font-size: 12px;
}

/* Цвета карточек */
.card-blue { border-color: #bfdbfe; }
.card-blue .card-icon { color: #3b82f6; }
.card-blue .card-title { color: #1d4ed8; }
.card-blue .card-subtitle { color: #60a5fa; }

.card-purple { border-color: #e9d5ff; }
.card-purple .card-icon { color: #a855f7; }
.card-purple .card-title { color: #7e22ce; }
.card-purple .card-subtitle { color: #c084fc; }

.card-green { border-color: #bbf7d0; }
.card-green .card-icon { color: #22c55e; }
.card-green .card-title { color: #15803d; }
.card-green .card-subtitle { color: #4ade80; }

.card-orange { border-color: #fed7aa; }
.card-orange .card-icon { color: #f97316; }
.card-orange .card-title { color: #c2410c; }
.card-orange .card-subtitle { color: #fb923c; }
</style>