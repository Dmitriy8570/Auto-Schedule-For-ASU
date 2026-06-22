import { ref } from 'vue'
import { ApiError } from '../api/http'

/**
 * Обёртка над асинхронной загрузкой: управляет состояниями loading/error и отменяет
 * предыдущий запрос при старте нового (AbortController) — устраняет дублирование try/catch
 * по табам и гонки, когда медленный ранний ответ перетирает поздний.
 */
export function useAsync() {
  const loading = ref(false)
  const error = ref<string | null>(null)
  let controller: AbortController | null = null

  async function run(action: (signal: AbortSignal) => Promise<void>): Promise<void> {
    controller?.abort()
    controller = new AbortController()
    const { signal } = controller

    loading.value = true
    error.value = null
    try {
      await action(signal)
    } catch (e) {
      if (signal.aborted) return // запрос отменён более новым — ошибку не показываем
      error.value = e instanceof ApiError ? e.message : 'Не удалось загрузить данные.'
    } finally {
      if (!signal.aborted) loading.value = false
    }
  }

  return { loading, error, run }
}
