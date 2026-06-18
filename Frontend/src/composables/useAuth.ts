import { computed } from 'vue'
import { token, currentUser, setSession, clearSession } from '../api/session'
import { login as loginRequest } from '../api/auth'

// Композабл аутентификации: единая точка для входа/выхода и текущего пользователя.
export function useAuth() {
  const isAuthenticated = computed(() => token.value !== null)

  async function login(username: string, password: string): Promise<void> {
    const response = await loginRequest(username, password)
    setSession(response)
  }

  function logout(): void {
    clearSession()
  }

  return {
    isAuthenticated,
    user: currentUser,
    login,
    logout,
  }
}
