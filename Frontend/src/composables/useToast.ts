import { ref } from 'vue'

export type ToastKind = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  kind: ToastKind
  message: string
}

// Модульное (singleton) состояние: все компоненты видят один список тостов.
const toasts = ref<Toast[]>([])
let seq = 0

function push(kind: ToastKind, message: string, ttlMs: number) {
  const id = ++seq
  toasts.value.push({ id, kind, message })
  if (ttlMs > 0) setTimeout(() => dismiss(id), ttlMs)
}

function dismiss(id: number) {
  toasts.value = toasts.value.filter(t => t.id !== id)
}

/** Тосты-уведомления для действий: success/info автоскрываются, ошибки висят дольше. */
export function useToast() {
  return {
    toasts,
    dismiss,
    success: (message: string) => push('success', message, 4000),
    info: (message: string) => push('info', message, 4000),
    error: (message: string) => push('error', message, 7000),
  }
}
