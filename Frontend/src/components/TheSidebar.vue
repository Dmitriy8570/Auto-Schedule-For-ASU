<script setup lang="ts">
import { Calendar, BarChart2, History, Settings, LogOut, Sparkles, AlertTriangle } from 'lucide-vue-next'
import { useRoute } from 'vue-router'

// Навигация по разделам через vue-router: каждый пункт ведёт на именованный маршрут, активный
// определяется текущим маршрутом. Выход пробрасывается родителю (он чистит сессию и SignalR).
const emit = defineEmits<{ (e: 'logout'): void }>()
const route = useRoute()

const items = [
  { name: 'schedule', label: 'Расписание', icon: Calendar },
  { name: 'load', label: 'Нагрузка', icon: BarChart2 },
  { name: 'unplaced', label: 'Нераспределённое', icon: AlertTriangle },
  { name: 'generation', label: 'Генерации', icon: Sparkles },
  { name: 'history', label: 'История', icon: History },
  { name: 'settings', label: 'Ограничения', icon: Settings },
]

const isActive = (name: string) => route.name === name
</script>

<template>
  <aside class="sidebar">

    <div class="logo-area">
      <Calendar :size="28" color="white" />
      <div class="text-group">
        <h2>Бюро расписаний</h2>
        <p>Университетская система</p>
      </div>
    </div>

    <nav class="nav-menu">
      <router-link
        v-for="item in items" :key="item.name"
        :to="{ name: item.name }"
        class="nav-item"
        :class="{ active: isActive(item.name) }"
      >
        <component :is="item.icon" :size="20" :color="isActive(item.name) ? '#1e4b8f' : 'white'" />
        <span>{{ item.label }}</span>
      </router-link>
    </nav>

    <div class="logout-area">
      <button class="nav-item logout-btn" @click="emit('logout')">
        <LogOut :size="20" color="white" />
        <span>Выйти</span>
      </button>
    </div>

  </aside>
</template>

<style scoped>
/* 1. Главный контейнер панели */
.sidebar {
  width: 260px;
  background-color: #1e4b8f;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

/* 2. Зона логотипа */
.logo-area {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

/* Текст логотипа */
.text-group h2 {
  color: white;
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.text-group p {
  color: rgba(255, 255, 255, 0.6);
  margin: 4px 0 0 0;
  font-size: 12px;
}

/* 3. Зона меню (Навигация) */
.nav-menu {
  flex-grow: 1; /*Заставляет это меню занять всё свободное место, выталкивая кнопку "Выйти" в самый низ экрана */
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 24px 16px;
}

/* Общий стиль для любой кнопки в меню */
.nav-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 16px;
  text-decoration: none;
  color: white;
  border-radius: 12px;
  font-size: 15px;
  font-weight: 500;
  transition: background-color 0.2s;
}

/* Эффект при наведении мышкой на неактивную кнопку */
.nav-item:hover:not(.active) {
  background-color: rgba(255, 255, 255, 0.1);
}

/* 4. Стиль для АКТИВНОЙ кнопки */
.nav-item.active {
  background-color: white;
  color: #1e4b8f;
  font-weight: 600;
}

/* 5. Зона выхода */
.logout-area {
  padding: 24px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.logout-btn {
  background: transparent;
  border: none;
  width: 100%;
  cursor: pointer;
  font-family: inherit;
}
</style>
