<script setup lang="ts">
import { useToast } from '../composables/useToast'

const { toasts, dismiss } = useToast()
</script>

<template>
  <div class="toast-host">
    <transition-group name="toast">
      <div v-for="t in toasts" :key="t.id" class="toast" :class="t.kind" role="status">
        <span class="toast-msg">{{ t.message }}</span>
        <button class="toast-close" aria-label="Закрыть" @click="dismiss(t.id)">×</button>
      </div>
    </transition-group>
  </div>
</template>

<style scoped>
.toast-host {
  position: fixed;
  top: 16px;
  right: 16px;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 380px;
}

.toast {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 10px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.18);
  font-size: 14px;
  color: #1e293b;
  background: white;
  border-left: 4px solid #94a3b8;
}

.toast.success { border-left-color: #22c55e; }
.toast.error { border-left-color: #dc2626; }
.toast.info { border-left-color: #3b82f6; }

.toast-msg { flex: 1; line-height: 1.4; }

.toast-close {
  background: none;
  border: none;
  font-size: 20px;
  line-height: 1;
  color: #94a3b8;
  cursor: pointer;
  padding: 0 2px;
}
.toast-close:hover { color: #475569; }

/* Анимация появления/исчезновения */
.toast-enter-active,
.toast-leave-active { transition: all 0.25s ease; }
.toast-enter-from { opacity: 0; transform: translateX(20px); }
.toast-leave-to { opacity: 0; transform: translateX(20px); }
</style>
