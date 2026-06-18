import { http } from './http'
import type { LoginResponse } from './types'

export function login(username: string, password: string): Promise<LoginResponse> {
  return http.post<LoginResponse>('/auth/login', { username, password })
}
