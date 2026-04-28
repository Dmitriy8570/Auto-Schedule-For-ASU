<script setup lang="ts">
// Подключаем типы для работы с v-model (двустороннее связывание данных)
defineProps<{
  modelValue: string;
  type?: string;
  placeholder?: string;
}>();

const emit = defineEmits(['update:modelValue']);
</script>

<template>
  <div class="input-wrapper">
    <div class="icon-left">
      <slot name="left-icon" />
    </div>

    <input
      :type="type || 'text'"
      :placeholder="placeholder"
      :value="modelValue"
      @input="(e: any) => emit('update:modelValue', e.target.value)"
    />

    <div class="icon-right">
      <slot name="right-icon" />
    </div>
  </div>
</template>

<style scoped>
.input-wrapper {
  position: relative;
  width: 100%;
}

input {
  width: 100%;
  height: 46px;
  padding: 0 40px; /* Место для обеих иконок */
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 14px;
  box-sizing: border-box;
  outline: none;
  transition: border-color 0.2s;
}

input:focus {
  border-color: #1a4d9c;
}

.icon-left, .icon-right {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
  display: flex;
  align-items: center;
}

.icon-left { left: 12px; }
.icon-right { right: 12px; cursor: pointer; }
</style>