<script setup lang="ts">
import TheHeader from './components/TheHeader.vue'
import LoginCard from './components/LoginCard.vue'
import TheFooter from './components/TheFooter.vue'
import MainDashboard from './components/MainDashboard.vue'
import { useAuth } from './composables/useAuth'

// Экран определяется наличием действительной сессии (JWT в localStorage).
const { isAuthenticated, logout } = useAuth()
</script>

<template>
  <div v-if="!isAuthenticated" class="login-page">
    <TheHeader />
    <LoginCard />
    <TheFooter />
  </div>

  <MainDashboard v-else @logout="logout" />
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  background-color: #1a4d9c;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 40px;
}
</style>