import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel,
} from '@microsoft/signalr'
import { token } from '../api/session'

// Полезная нагрузка события об изменении расписания (см. бэкенд SignalRRealtimeNotifier).
export interface ScheduleChangedPayload { instituteId: string; changeType: string }
export interface WorkloadChangedPayload { added: number; updated: number; deleted: number }

// Стор реального времени (SignalR). Держит единственное соединение с хабом `/hubs/schedule`
// и публикует входящие события как реактивные сигналы: компоненты следят за «тиками» и
// перезагружают данные без поллинга. Канал односторонний (сервер → клиент), методов клиента нет.
export const useRealtimeStore = defineStore('realtime', () => {
  const connected = ref(false)
  // «Тики» инкрементируются на каждом событии — на них удобно вешать watch без сравнения payload.
  const scheduleTick = ref(0)
  const workloadTick = ref(0)
  const lastSchedule = ref<ScheduleChangedPayload | null>(null)
  const lastWorkload = ref<WorkloadChangedPayload | null>(null)

  let conn: HubConnection | null = null

  function build(): HubConnection {
    const c = new HubConnectionBuilder()
      // Токен передаётся клиентом SignalR в query `access_token` для WebSocket — бэкенд его читает.
      .withUrl('/hubs/schedule', { accessTokenFactory: () => token.value ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    c.on('ScheduleChanged', (p: ScheduleChangedPayload) => { lastSchedule.value = p; scheduleTick.value++ })
    c.on('WorkloadChanged', (p: WorkloadChangedPayload) => { lastWorkload.value = p; workloadTick.value++ })

    c.onreconnected(() => { connected.value = true })
    c.onreconnecting(() => { connected.value = false })
    c.onclose(() => { connected.value = false })
    return c
  }

  async function connect(): Promise<void> {
    if (!token.value) return
    if (conn && conn.state !== HubConnectionState.Disconnected) return
    conn ??= build()
    try {
      await conn.start()
      connected.value = true
    } catch {
      // Сбой соединения не должен мешать работе — REST остаётся источником истины.
      connected.value = false
    }
  }

  async function disconnect(): Promise<void> {
    if (!conn) return
    try { await conn.stop() } catch { /* игнорируем */ }
    conn = null
    connected.value = false
  }

  return {
    connected, scheduleTick, workloadTick, lastSchedule, lastWorkload, connect, disconnect,
  }
})
