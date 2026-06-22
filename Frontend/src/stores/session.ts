import { defineStore } from 'pinia'
import { computed } from 'vue'
import { token, currentUser, setSession, clearSession } from '../api/session'
import { login as loginRequest } from '../api/auth'

// Стор сессии (Pinia). Низкоуровневое хранилище токена/пользователя живёт в `api/session.ts`
// (его импортирует `http.ts` — отдельный модуль разрывает циклические зависимости). Этот стор —
// единая точка для компонентов и навигационного гварда: вход/выход и признак аутентификации.
export const useSessionStore = defineStore('session', () => {
  const isAuthenticated = computed(() => token.value !== null)

  async function login(username: string, password: string): Promise<void> {
    setSession(await loginRequest(username, password))
  }

  function logout(): void {
    clearSession()
  }

  return { token, user: currentUser, isAuthenticated, login, logout }
})
