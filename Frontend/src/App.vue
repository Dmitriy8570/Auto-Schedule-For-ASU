<script setup lang="ts">
import { watch, onMounted } from 'vue'
import ToastHost from './components/ToastHost.vue'
import { useSessionStore } from './stores/session'
import { useRealtimeStore } from './stores/realtime'

// Корень приложения: только маршрутизируемое представление + хост тостов. Экран входа и дашборд —
// это маршруты (см. router). Здесь же — жизненный цикл SignalR-соединения: подключаемся при
// наличии сессии и переподключаемся/отключаемся при входе/выходе.
const session = useSessionStore()
const realtime = useRealtimeStore()

onMounted(() => { if (session.isAuthenticated) realtime.connect() })

watch(() => session.isAuthenticated, (authed) => {
  if (authed) realtime.connect()
  else realtime.disconnect()
})
</script>

<template>
  <router-view />
  <ToastHost />
</template>
