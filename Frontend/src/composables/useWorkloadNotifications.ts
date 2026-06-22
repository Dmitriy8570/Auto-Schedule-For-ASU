import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { workloads } from '../api/workloads'
import { useRealtimeStore } from '../stores/realtime'
import type { WorkloadChangeDto } from '../api/types'

// Уведомления об изменении нагрузки. Источник — журнал изменений (`/workloads/changes`),
// который наполняется при синхронизации с ММИС. «Непрочитанные» — изменения новее отметки
// последнего просмотра (хранится в localStorage, переживает перезагрузку). Обновление —
// мгновенное по событию SignalR `WorkloadChanged`, с лёгким периодическим поллингом как запасным
// каналом на случай разрыва соединения.
const LS_LAST_SEEN = 'notif:workload:lastSeenUtc'
const POLL_INTERVAL_MS = 60_000
const FEED_SIZE = 20

const epoch = (iso: string): number => Date.parse(iso) || 0

export function useWorkloadNotifications() {
  const items = ref<WorkloadChangeDto[]>([])
  const loading = ref(false)
  const lastSeen = ref<string>(localStorage.getItem(LS_LAST_SEEN) ?? '')
  let timer: number | undefined

  const unreadCount = computed(() => {
    const seen = lastSeen.value ? epoch(lastSeen.value) : 0
    return items.value.filter(i => epoch(i.timeStamp) > seen).length
  })

  const isUnread = (i: WorkloadChangeDto): boolean =>
    epoch(i.timeStamp) > (lastSeen.value ? epoch(lastSeen.value) : 0)

  async function refresh() {
    loading.value = true
    try {
      const res = await workloads.changes({ page: 1, pageSize: FEED_SIZE })
      items.value = res.items
    } catch {
      /* молча: уведомления не должны мешать основной работе */
    } finally {
      loading.value = false
    }
  }

  // Пометить всё прочитанным: отметка = самый свежий timestamp текущей ленты.
  function markAllRead() {
    let newest = ''
    let newestT = 0
    for (const i of items.value) {
      const t = epoch(i.timeStamp)
      if (t > newestT) { newestT = t; newest = i.timeStamp }
    }
    if (newest) {
      lastSeen.value = newest
      localStorage.setItem(LS_LAST_SEEN, newest)
    }
  }

  // Мгновенное обновление по событию реального времени.
  const realtime = useRealtimeStore()
  watch(() => realtime.workloadTick, refresh)

  onMounted(() => {
    refresh()
    timer = window.setInterval(refresh, POLL_INTERVAL_MS)
  })
  onUnmounted(() => { if (timer) window.clearInterval(timer) })

  return { items, loading, unreadCount, isUnread, refresh, markAllRead }
}
