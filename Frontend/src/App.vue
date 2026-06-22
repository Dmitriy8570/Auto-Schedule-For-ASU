<script setup lang="ts">
import TheHeader from './components/TheHeader.vue'
import LoginCard from './components/LoginCard.vue'
import MainDashboard from './components/MainDashboard.vue'
import ToastHost from './components/ToastHost.vue'
import { useAuth } from './composables/useAuth'

// Экран определяется наличием действительной сессии (JWT в localStorage).
const { isAuthenticated, logout } = useAuth()
</script>

<template>
  <div v-if="!isAuthenticated" class="login-page">
    <TheHeader />
    <LoginCard />
  </div>

  <MainDashboard v-else @logout="logout" />

  <ToastHost />
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  /* Градиент из макета Figma (from-rgb(26,77,156) via-rgb(35,95,180) to-rgb(20,60,130)) */
  background: linear-gradient(135deg, #1a4d9c 0%, #235fb4 50%, #143c82 100%);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 40px;
}
</style>