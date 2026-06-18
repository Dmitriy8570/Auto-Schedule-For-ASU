import { ref } from 'vue'
import type { LoginResponse } from './types'

// Текущая сессия: JWT-токен и сведения о пользователе. Состояние реактивно и
// синхронизировано с localStorage, чтобы переживать перезагрузку страницы.
// Модуль намеренно не зависит от http/api — это разрывает циклические импорты.

const TOKEN_KEY = 'auth.token'
const USER_KEY = 'auth.user'

function readUser(): LoginResponse | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  try { return JSON.parse(raw) as LoginResponse } catch { return null }
}

export const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
export const currentUser = ref<LoginResponse | null>(readUser())

export function setSession(response: LoginResponse): void {
  token.value = response.token
  currentUser.value = response
  localStorage.setItem(TOKEN_KEY, response.token)
  localStorage.setItem(USER_KEY, JSON.stringify(response))
}

export function clearSession(): void {
  token.value = null
  currentUser.value = null
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}
