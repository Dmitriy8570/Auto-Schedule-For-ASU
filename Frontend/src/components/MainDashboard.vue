<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TheSidebar from './TheSidebar.vue'
import NotificationBell from './NotificationBell.vue'
import { useSessionStore } from '../stores/session'
import { useRealtimeStore } from '../stores/realtime'

// Layout дашборда: сайдбар + шапка + маршрутизируемое содержимое вкладки (router-view).
// Активная вкладка и заголовок шапки определяются текущим маршрутом.
const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const realtime = useRealtimeStore()

const title = computed(() => (route.meta.title as string | undefined) ?? '')

function logout() {
  realtime.disconnect()
  session.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="dashboard-layout">

    <TheSidebar @logout="logout" />

    <div class="main-content">
      <header class="content-topbar">
        <h1 class="topbar-title">{{ title }}</h1>
        <NotificationBell @open-history="router.push({ name: 'history' })" />
      </header>

      <router-view />
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

/* Шапка контента с заголовком вкладки и колокольчиком уведомлений */
.content-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.topbar-title {
  margin: 0;
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
}
</style>
