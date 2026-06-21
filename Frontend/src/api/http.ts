import { token, clearSession } from './session'

// Базовый префикс. В dev он проксируется vite на бэкенд, в проде — nginx.
const BASE = '/api'

/** Ошибка обращения к API с человекочитаемым сообщением и HTTP-статусом. */
export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** query-параметры; undefined/null/'' пропускаются. */
  query?: Record<string, string | number | boolean | null | undefined>
  signal?: AbortSignal
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = BASE + path
  if (!query) return url
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue
    params.append(key, String(value))
  }
  const qs = params.toString()
  return qs ? `${url}?${qs}` : url
}

async function extractError(response: Response): Promise<string> {
  // Бэкенд отдаёт ProblemDetails / ValidationProblemDetails.
  try {
    const data = await response.json()
    if (data?.errors && typeof data.errors === 'object') {
      const messages = Object.values(data.errors as Record<string, string[]>).flat()
      if (messages.length) return messages.join('; ')
    }
    // 409 коллизия расписания: ProblemDetails с массивом conflicts[{kind, detail}].
    if (Array.isArray(data?.conflicts) && data.conflicts.length) {
      const details = (data.conflicts as Array<{ detail?: string }>)
        .map(c => c?.detail).filter(Boolean)
      if (details.length) return `Коллизия: ${details.join(' ')}`
    }
    if (typeof data?.detail === 'string') return data.detail
    if (typeof data?.title === 'string') return data.title
  } catch {
    /* тело не JSON — используем статус ниже */
  }
  if (response.status === 401) return 'Требуется вход в систему.'
  if (response.status === 403) return 'Недостаточно прав.'
  return `Ошибка запроса (${response.status}).`
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {}
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (token.value) headers['Authorization'] = `Bearer ${token.value}`

  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? 'GET',
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    signal: options.signal,
  })

  // Истёкший/невалидный токен — завершаем сессию, UI вернётся на экран входа.
  if (response.status === 401) {
    clearSession()
    throw new ApiError(401, await extractError(response))
  }

  if (!response.ok) throw new ApiError(response.status, await extractError(response))

  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const http = {
  get: <T>(path: string, query?: RequestOptions['query'], signal?: AbortSignal) =>
    request<T>(path, { method: 'GET', query, signal }),
  post: <T>(path: string, body?: unknown, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'POST', body, query }),
  put: <T>(path: string, body?: unknown, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'PUT', body, query }),
  del: <T>(path: string, query?: RequestOptions['query']) =>
    request<T>(path, { method: 'DELETE', query }),
}
