<script setup lang="ts">
// Описываем, какие настройки может принимать кнопка
interface Props {
  variant?: 'primary' | 'outline' | 'gradient' | 'light-grey';
  disabled?: boolean;
}

// Задаем значения по умолчанию
withDefaults(defineProps<Props>(), {
  variant: 'primary'
});
</script>

<template>
  <button 
    class="base-btn" 
    :class="[variant, { disabled }]" 
    :disabled="disabled"
  >
    <slot />
  </button>
</template>

<style scoped>
.base-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 10px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  font-family: inherit;
}

/* Синяя кнопка (как в логине или активных вкладках) */
.primary {
  background-color: #1a4d9c;
  color: white;
}
.primary:hover:not(.disabled) { background-color: #143c82; }

/* Кнопка с контуром (Сбросить, Выгрузить) */
.outline {
  background: white;
  border: 1px solid #cbd5e1;
  color: #475569;
}
.outline:hover:not(.disabled) { background-color: #f8fafc; }

/* Фиолетовый градиент (Автогенерация) */
.gradient {
  background: linear-gradient(90deg, #9333ea 0%, #2563eb 100%);
  color: white;
}
.gradient:hover:not(.disabled) { opacity: 0.9; }

/* Светло-серая (Добавить пару) */
.light-grey {
  background-color: #f1f5f9;
  color: #94a3b8;
}

.disabled {
  cursor: not-allowed;
  opacity: 0.6;
}
</style>