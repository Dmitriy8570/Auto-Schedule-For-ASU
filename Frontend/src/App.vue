<script setup lang="ts">
import TheHeader from './components/TheHeader.vue'
import LoginCard from './components/LoginCard.vue'
import TheFooter from './components/TheFooter.vue'
</script>

<template>
  <div class="app-layout">
    
    <TheHeader />
    
    <LoginCard />
    
    <TheFooter />
    
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  background-color: #1e56a8;

  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;

  gap: 40px;
}
</style>