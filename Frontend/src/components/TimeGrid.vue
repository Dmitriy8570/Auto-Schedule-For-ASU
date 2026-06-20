<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import type { AvailabilityState } from '../api/types'

// Сетка доступности «день × пара» с заливкой перетаскиванием и 5 градациями желательности.
// v-model — словарь { `${day}-${pair}`: AvailabilityState }; отсутствующая ячейка = Neutral.
const props = defineProps<{ modelValue: Record<string, AvailabilityState> }>()
const emit = defineEmits<{ (e: 'update:modelValue', value: Record<string, AvailabilityState>): void }>()

const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб']
const PERIODS = [1, 2, 3, 4, 5, 6, 7, 8]
const PERIOD_TIMES: Record<number, string> = {
  1: '08:00', 2: '09:40', 3: '11:30', 4: '13:10',
  5: '15:00', 6: '16:40', 7: '18:20', 8: '20:00',
}
// Порядок переключения по клику: нейтр. → предпочт. → обязат. → нежел. → запрещено.
const CELL_CYCLE: AvailabilityState[] = ['Neutral', 'Preferred', 'Required', 'Discouraged', 'Prohibited']
const STYLES: Record<AvailabilityState, { bg: string; border: string; label: string }> = {
  Required:    { bg: '#22c55e', border: '#16a34a', label: 'Обязат.' },
  Preferred:   { bg: '#bbf7d0', border: '#4ade80', label: 'Предпочт.' },
  Neutral:     { bg: '#f1f5f9', border: '#e2e8f0', label: 'Нейтр.' },
  Discouraged: { bg: '#fef08a', border: '#facc15', label: 'Нежел.' },
  Prohibited:  { bg: '#f87171', border: '#ef4444', label: 'Запрещено' },
}
const FILL_OPTIONS: AvailabilityState[] = ['Neutral', 'Preferred', 'Required', 'Prohibited']

const key = (day: number, pair: number) => `${day}-${pair}`
const stateAt = (day: number, pair: number): AvailabilityState => props.modelValue[key(day, pair)] ?? 'Neutral'

const isDragging = ref(false)
const dragState = ref<AvailabilityState>('Neutral')

const cycle = (s: AvailabilityState): AvailabilityState =>
  CELL_CYCLE[(CELL_CYCLE.indexOf(s) + 1) % CELL_CYCLE.length] ?? 'Neutral'

function setCell(day: number, pair: number, s: AvailabilityState) {
  emit('update:modelValue', { ...props.modelValue, [key(day, pair)]: s })
}
function onMouseDown(day: number, pair: number) {
  const next = cycle(stateAt(day, pair))
  dragState.value = next
  isDragging.value = true
  setCell(day, pair, next)
}
function onMouseEnter(day: number, pair: number) {
  if (isDragging.value) setCell(day, pair, dragState.value)
}
function fillAll(s: AvailabilityState) {
  const grid: Record<string, AvailabilityState> = {}
  for (let d = 0; d < DAYS.length; d++) for (const p of PERIODS) grid[key(d, p)] = s
  emit('update:modelValue', grid)
}

const stopDrag = () => { isDragging.value = false }
onMounted(() => window.addEventListener('mouseup', stopDrag))
onBeforeUnmount(() => window.removeEventListener('mouseup', stopDrag))
</script>

<template>
  <div class="time-grid" @mouseleave="isDragging = false">
    <div class="legend">
      <div v-for="(s, st) in STYLES" :key="st" class="legend-item">
        <span class="swatch" :style="{ backgroundColor: s.bg, borderColor: s.border }" />
        <span class="legend-label">{{ s.label }}</span>
      </div>
    </div>

    <div class="fill-row">
      <span class="fill-hint">Заполнить:</span>
      <button
        v-for="s in FILL_OPTIONS" :key="s" type="button" class="fill-btn"
        :style="{ backgroundColor: STYLES[s].bg, borderColor: STYLES[s].border }"
        @click="fillAll(s)"
      >{{ STYLES[s].label }}</button>
    </div>

    <div class="grid-scroll">
      <div class="grid-inner">
        <div class="grid-head">
          <div class="corner" />
          <div v-for="d in DAYS" :key="d" class="day-head">{{ d }}</div>
        </div>
        <div v-for="p in PERIODS" :key="p" class="grid-row">
          <div class="period-head">
            <span class="period-num">{{ p }}п</span>
            <span class="period-time">{{ PERIOD_TIMES[p] }}</span>
          </div>
          <div
            v-for="(_, di) in DAYS" :key="di"
            class="cell"
            :style="{ backgroundColor: STYLES[stateAt(di, p)].bg, borderColor: STYLES[stateAt(di, p)].border }"
            :title="STYLES[stateAt(di, p)].label"
            @mousedown.prevent="onMouseDown(di, p)"
            @mouseenter="onMouseEnter(di, p)"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.time-grid { user-select: none; }

.legend { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 10px; }
.legend-item { display: flex; align-items: center; gap: 5px; }
.swatch { width: 11px; height: 11px; border-radius: 3px; border: 1px solid; display: inline-block; }
.legend-label { font-size: 11px; color: #64748b; }

.fill-row { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; margin-bottom: 14px; }
.fill-hint { font-size: 11px; color: #94a3b8; }
.fill-btn { font-size: 10px; padding: 2px 8px; border-radius: 6px; border: 1px solid; color: #334155; cursor: pointer; }
.fill-btn:hover { opacity: 0.8; }

.grid-scroll { overflow-x: auto; }
.grid-inner { min-width: max-content; }
.grid-head { display: flex; margin-bottom: 4px; }
.corner { width: 52px; flex-shrink: 0; }
.day-head { width: 34px; text-align: center; font-size: 11px; font-weight: 600; color: #64748b; }

.grid-row { display: flex; align-items: center; margin-bottom: 3px; }
.period-head { width: 52px; flex-shrink: 0; padding-right: 6px; text-align: right; }
.period-num { font-size: 10px; font-weight: 600; color: #64748b; }
.period-time { font-size: 9px; color: #94a3b8; display: block; }

.cell {
  width: 34px; height: 28px; margin: 0 2px; border-radius: 5px;
  border: 2px solid; cursor: pointer; transition: transform 0.1s, box-shadow 0.1s;
}
.cell:hover { transform: scale(1.06); box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12); }
</style>
