<script setup lang="ts">
import { ref } from 'vue'
import { Bell, PlusCircle, RotateCw, Trash2, Clock, X } from 'lucide-vue-next'
import { useWorkloadNotifications } from '../composables/useWorkloadNotifications'
import type { LogAction } from '../api/types'

const emit = defineEmits<{ (e: 'open-history'): void }>()

const { items, loading, unreadCount, isUnread, markAllRead } = useWorkloadNotifications()

const open = ref(false)

const actionMeta: Record<LogAction, { title: string; type: string; icon: typeof PlusCircle }> = {
  Add: { title: 'Добавлена нагрузка', type: 'add', icon: PlusCircle },
  Update: { title: 'Изменена нагрузка', type: 'edit', icon: RotateCw },
  Delete: { title: 'Удалена нагрузка', type: 'delete', icon: Trash2 },
}

const formatTime = (iso: string): string => {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString('ru-RU')
}

function toggle() {
  open.value = !open.value
  // Открытие = просмотр: гасим бейдж непрочитанного.
  if (open.value) markAllRead()
}

function goHistory() {
  open.value = false
  emit('open-history')
}
</script>

<template>
  <div class="bell-wrap">
    <button class="bell-btn" :class="{ active: open }" @click="toggle" title="Уведомления об изменении нагрузки">
      <Bell :size="20" />
      <span v-if="unreadCount > 0" class="bell-badge">{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
    </button>

    <!-- Затемнение-перехватчик клика вне меню -->
    <div v-if="open" class="bell-scrim" @click="open = false"></div>

    <div v-if="open" class="bell-menu">
      <header class="bm-header">
        <div class="bm-title"><Bell :size="16" /> Уведомления о нагрузке</div>
        <button class="bm-close" @click="open = false"><X :size="16" /></button>
      </header>

      <div class="bm-list">
        <div v-if="loading && items.length === 0" class="bm-empty">Загрузка…</div>
        <div v-else-if="items.length === 0" class="bm-empty">Изменений нагрузки нет.</div>

        <div v-else v-for="(event, idx) in items" :key="idx" class="bm-item" :class="{ unread: isUnread(event) }">
          <div class="bm-icon" :class="`bg-${actionMeta[event.action].type}`">
            <component :is="actionMeta[event.action].icon" :size="16" :class="`ic-${actionMeta[event.action].type}`" />
          </div>
          <div class="bm-content">
            <div class="bm-line">{{ actionMeta[event.action].title }}</div>
            <div class="bm-sub">
              Часы: {{ event.oldValue }} → {{ event.newValue }}
              <span class="bm-scope">{{ event.scope === 'Semester' ? 'Семестр' : 'Неделя' }}</span>
            </div>
            <div class="bm-time"><Clock :size="12" /> {{ formatTime(event.timeStamp) }}</div>
          </div>
        </div>
      </div>

      <footer class="bm-footer">
        <button class="bm-all" @click="goHistory">Вся история изменений →</button>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.bell-wrap { position: relative; }

.bell-btn { position: relative; width: 44px; height: 44px; border-radius: 12px; border: 1px solid #e2e8f0; background: white; color: #475569; display: flex; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s; }
.bell-btn:hover, .bell-btn.active { background: #f1f5f9; color: #1a4d9c; border-color: #cbd5e1; }
.bell-badge { position: absolute; top: -6px; right: -6px; min-width: 18px; height: 18px; padding: 0 5px; border-radius: 9999px; background: #dc2626; color: white; font-size: 11px; font-weight: 700; display: flex; align-items: center; justify-content: center; box-shadow: 0 0 0 2px white; }

.bell-scrim { position: fixed; inset: 0; z-index: 90; }

.bell-menu { position: absolute; top: calc(100% + 10px); right: 0; width: 360px; max-width: 90vw; background: white; border: 1px solid #e2e8f0; border-radius: 14px; box-shadow: 0 12px 28px rgba(0,0,0,0.14); z-index: 95; display: flex; flex-direction: column; overflow: hidden; }
.bm-header { display: flex; align-items: center; justify-content: space-between; padding: 14px 16px; border-bottom: 1px solid #f1f5f9; }
.bm-title { display: inline-flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 600; color: #1e293b; }
.bm-close { background: none; border: none; color: #94a3b8; cursor: pointer; display: flex; padding: 2px; border-radius: 6px; }
.bm-close:hover { background: #f1f5f9; color: #475569; }

.bm-list { max-height: 380px; overflow-y: auto; }
.bm-empty { padding: 28px 16px; text-align: center; color: #94a3b8; font-size: 14px; }

.bm-item { display: flex; gap: 12px; padding: 12px 16px; border-bottom: 1px solid #f8fafc; }
.bm-item.unread { background: #eff6ff; }
.bm-icon { width: 32px; height: 32px; border-radius: 9999px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.bg-add { background: #f0fdf4; } .ic-add { color: #16a34a; }
.bg-delete { background: #fef2f2; } .ic-delete { color: #dc2626; }
.bg-edit { background: #eff6ff; } .ic-edit { color: #2563eb; }
.bm-content { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.bm-line { font-size: 14px; font-weight: 600; color: #1e293b; }
.bm-sub { font-size: 13px; color: #475569; display: flex; align-items: center; gap: 8px; }
.bm-scope { background: #f1f5f9; color: #64748b; padding: 1px 8px; border-radius: 9999px; font-size: 11px; font-weight: 600; }
.bm-time { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; color: #94a3b8; margin-top: 2px; }

.bm-footer { padding: 10px 16px; border-top: 1px solid #f1f5f9; background: #f8fafc; }
.bm-all { width: 100%; background: none; border: none; color: #1a4d9c; font-size: 13px; font-weight: 600; cursor: pointer; padding: 6px; border-radius: 8px; }
.bm-all:hover { background: #eff6ff; }
</style>
