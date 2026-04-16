<script setup lang="ts">
    import { Calendar, GraduationCap } from 'lucide-vue-next'
</script>

<template>
  <div class="header">
    
    <div class="icon-placeholder">
      <Calendar :size="36" color="#1a4d9c" />
    </div> 
    
    <h1>Система расписаний</h1>
    
    <p class="subtitle">
      <GraduationCap :size="16" /> 
      Бюро расписаний университета
    </p>
    
  </div>
</template>

<style scoped>
.header {
  text-align: center;
  color: white;
}

h1 {
  margin: 10px 0 0 0;
  font-size: 32px;
}

/* Обновляем стиль для подзаголовка, чтобы иконка и текст стояли ровно в одну линию */
.subtitle {
  margin: 8px 0 0 0;
  font-size: 14px;
  opacity: 0.9;
  display: flex; /* Выстраиваем в ряд */
  align-items: center; /* Центрируем по вертикали */
  justify-content: center; /* Центрируем по горизонтали */
  gap: 6px; /* Делаем небольшой отступ между иконкой и текстом */
}

/* Белый квадратик для календаря */
.icon-placeholder {
  background: white;
  display: inline-flex; /* Изменили на inline-flex для идеального центрирования иконки */
  align-items: center;
  justify-content: center;
  padding: 15px;
  border-radius: 16px;
}
</style>