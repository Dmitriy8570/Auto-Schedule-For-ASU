<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { User, Lock, Eye, EyeOff } from 'lucide-vue-next'
import BaseButton from './BaseButton.vue'
import BaseInput from './BaseInput.vue'
import { useSessionStore } from '../stores/session'
import { ApiError } from '../api/http'

const session = useSessionStore()
const router = useRouter()

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

async function submit() {
  if (loading.value) return
  error.value = ''

  if (!username.value.trim() || !password.value) {
    error.value = 'Введите имя пользователя и пароль.'
    return
  }

  loading.value = true
  try {
    await session.login(username.value.trim(), password.value)
    router.push({ name: 'schedule' }) // успех: уходим на дашборд (гвард пропустит)
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : 'Не удалось войти. Попробуйте позже.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <form class="login-card" @submit.prevent="submit">
    <h2>Вход в систему</h2>

    <div class="form-group">
      <label>Имя пользователя</label>
      <BaseInput v-model="username" placeholder="Введите имя">
        <template #left-icon><User :size="18" /></template>
      </BaseInput>
    </div>

    <div class="form-group">
      <label>Пароль</label>
      <BaseInput
        v-model="password"
        :type="showPassword ? 'text' : 'password'"
        placeholder="Введите пароль"
      >
        <template #left-icon><Lock :size="18" /></template>
        <template #right-icon>
          <component :is="showPassword ? EyeOff : Eye" :size="18" @click="showPassword = !showPassword" />
        </template>
      </BaseInput>
    </div>

    <p v-if="error" class="login-error">{{ error }}</p>

    <BaseButton variant="primary" :disabled="loading" type="submit">
      {{ loading ? 'Вход…' : 'Войти' }}
    </BaseButton>
  </form>
</template>

<style scoped>
/* Сама карточка */
.login-card {
  background-color: white;
  width: 450px; 
  padding: 50px; 
  border-radius: 16px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.05);
  box-sizing: border-box; /* Важно: чтобы padding не увеличивал ширину карточки */

  display: flex;
  flex-direction: column;
  justify-content: center; /* Оставляет элементы по центру по вертикали */
  align-items: stretch;
}

h2 {
  margin: 0 0 24px 0;
  color: #1e293b;
  font-size: 22px;
  font-weight: 700;
}

.login-error {
  margin: 0 0 16px 0;
  padding: 10px 12px;
  background-color: #fef2f2;
  border: 1px solid #fca5a5;
  border-radius: 8px;
  color: #dc2626;
  font-size: 13px;
}

/* Контейнер для каждого поля ввода с текстом над ним */
.form-group {
  width: 100%; /* Принудительно говорим занимать всё место */
  margin-bottom: 20px;
}

/* Текст "Имя пользователя" и "Пароль" */
label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 8px;
}

/* Обертка для инпута, чтобы позиционировать иконки внутри */
.input-wrapper {
  position: relative;
}

/* Сами поля ввода */
input {
  width: 100%;
  height: 46px; /* Точная высота по макету */
  padding-left: 40px; /* Место для левой иконки */
  padding-right: 40px; /* Место для правой иконки */
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 14px;
  color: #0f172a;
  outline: none; /* Убираем стандартную черную рамку при клике */
  box-sizing: border-box;
  transition: border-color 0.2s; /* Плавная смена цвета рамки */
}

input:focus {
  border-color: #1a4d9c; /* Синяя рамка при вводе текста */
}

input::placeholder {
  color: #94a3b8; /* Светлый текст подсказки */
}

/* Иконки внутри полей */
.icon-left, .icon-right {
  position: absolute;
  top: 50%;
  transform: translateY(-50%); /* Выравниваем ровно по центру по вертикали */
  color: #94a3b8;
  font-size: 16px;
}

.icon-left { left: 12px; }
.icon-right { right: 12px; cursor: pointer; }

/* Синяя кнопка */
.submit-btn {
  width: 100%;
  height: 46px;
  background-color: #1a4d9c;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  margin-top: 8px;
  margin-bottom: 30px; /* Отступ до демо-блока */
  transition: background-color 0.2s;
}

.submit-btn:hover {
  background-color: #143c82; /* Чуть темнеет при наведении */
}
</style>

