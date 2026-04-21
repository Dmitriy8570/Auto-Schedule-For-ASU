<script setup lang="ts">
import { ref } from 'vue'
import TheHeader from './components/TheHeader.vue'
import LoginCard from './components/LoginCard.vue'
import TheFooter from './components/TheFooter.vue'
import MainDashboard from './components/MainDashboard.vue'


const isLoggedIn = ref(false)

// Функция входа
const handleLogin = () => {
  isLoggedIn.value = true
}

const handleLogout = () => {
  isLoggedIn.value = false
}
</script>

<template>
  <div v-if="!isLoggedIn" class="login-page">
    <TheHeader />
    <LoginCard @login="handleLogin" />
    <TheFooter />
  </div>

  <MainDashboard v-else @logout="handleLogout" />
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