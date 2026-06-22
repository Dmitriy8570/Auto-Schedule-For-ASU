<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { Sparkles, ChevronDown, ChevronRight, AlertTriangle, CheckCircle2, XCircle } from 'lucide-vue-next'
import BaseSelect, { type SelectOption } from './BaseSelect.vue'
import { lessons } from '../api/lessons'
import { lookups } from '../api/lookups'
import { calendar } from '../api/calendar'
import { useAsync } from '../composables/useAsync'
import type { InstituteDto, SemesterDto, GenerationRunDto, LessonType } from '../api/types'

const semesters = ref<SemesterDto[]>([])
const institutes = ref<InstituteDto[]>([])
const selSemester = ref('')
const selInstitute = ref('')

const runs = ref<GenerationRunDto[]>([])
const { loading, error, run } = useAsync()
const expanded = ref<Set<string>>(new Set())

// Опции для выпадающих списков с поиском (BaseSelect).
const semesterOptions = computed<SelectOption[]>(() => semesters.value.map(s => ({
  value: s.id, label: `${s.startDate} — ${s.endDate}${s.isCurrent ? ' (текущий)' : ''}`,
})))
const instituteOptions = computed<SelectOption[]>(() => institutes.value.map(i => ({ value: i.id, label: i.name })))

const lessonTypeLabel: Record<LessonType, string> = {
  Lecture: 'Лекция', Seminar: 'Практика', Laboratory: 'Лаб.', Consultation: 'Консульт.', Examination: 'Экзамен',
}

function toggle(id: string) {
  const next = new Set(expanded.value)
  next.has(id) ? next.delete(id) : next.add(id)
  expanded.value = next
}

// Класс бейджа статуса: ошибка/без занятий — красный, частично — жёлтый, успех — зелёный.
function statusKind(r: GenerationRunDto): 'ok' | 'warn' | 'err' {
  if (!r.succeeded) return 'err'
  if (r.unplacedCount > 0 || r.status.startsWith('Partial')) return 'warn'
  if (r.status === 'Infeasible' || r.status === 'Empty') return 'err'
  return 'ok'
}

const fmtDate = (iso: string) =>
  new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })

async function load() {
  await run(async () => {
    runs.value = await lessons.generationHistory({
      semesterId: selSemester.value || undefined,
      instituteId: selInstitute.value || undefined,
    })
  })
  if (error.value) runs.value = []
}

watch([selSemester, selInstitute], load)

onMounted(async () => {
  [semesters.value, institutes.value] = await Promise.all([
    calendar.semesters().catch(() => []),
    lookups.institutes().catch(() => []),
  ])
  await load()
})
</script>

<template>
  <div class="gh-container">
    <header class="gh-header">
      <div class="title-group">
        <Sparkles :size="24" color="#1a4d9c" />
        <h2>История автогенерации</h2>
      </div>
      <button class="gh-refresh" :disabled="loading" @click="load">Обновить</button>
    </header>

    <div class="filters-bar">
      <div class="filter-group">
        <label>Семестр</label>
        <BaseSelect v-model="selSemester" :options="semesterOptions"
                    placeholder="Все семестры" clear-label="Все семестры" search-placeholder="Поиск семестра…" />
      </div>
      <div class="filter-group">
        <label>Институт</label>
        <BaseSelect v-model="selInstitute" :options="instituteOptions"
                    placeholder="Все институты" clear-label="Все институты" search-placeholder="Поиск института…" />
      </div>
    </div>

    <div class="table-wrapper">
      <div v-if="loading" class="state-msg">Загрузка…</div>
      <div v-else-if="error" class="state-msg state-error">{{ error }}</div>
      <div v-else-if="runs.length === 0" class="state-msg">Запусков генерации пока нет.</div>

      <table v-else class="gh-table">
        <thead>
          <tr>
            <th class="gh-arrow-th"></th>
            <th>Когда</th>
            <th>Институт</th>
            <th>Семестр</th>
            <th>Статус</th>
            <th class="gh-num">Занятий</th>
            <th class="gh-num">Не размещено</th>
            <th class="gh-num">Время</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="r in runs" :key="r.id">
            <tr class="gh-row" :class="{ 'has-detail': r.unplacedCount > 0 }" @click="r.unplacedCount > 0 && toggle(r.id)">
              <td class="gh-arrow">
                <component :is="expanded.has(r.id) ? ChevronDown : ChevronRight" v-if="r.unplacedCount > 0" :size="16" />
              </td>
              <td class="gh-when">{{ fmtDate(r.completedAt) }}</td>
              <td class="gh-inst">{{ r.instituteName }}</td>
              <td class="gh-sem">{{ r.semesterName }}</td>
              <td>
                <span class="gh-badge" :class="`gh-${statusKind(r)}`">
                  <CheckCircle2 v-if="statusKind(r) === 'ok'" :size="13" />
                  <AlertTriangle v-else-if="statusKind(r) === 'warn'" :size="13" />
                  <XCircle v-else :size="13" />
                  {{ r.succeeded ? r.status : 'Ошибка' }}
                </span>
              </td>
              <td class="gh-num"><b>{{ r.lessonsCreated }}</b></td>
              <td class="gh-num">
                <span v-if="r.unplacedCount > 0" class="gh-unplaced">{{ r.unplacedCount }}</span>
                <span v-else class="gh-dash">—</span>
              </td>
              <td class="gh-num">{{ r.wallTimeSeconds.toFixed(1) }} с</td>
            </tr>

            <tr v-if="expanded.has(r.id)" :key="r.id + '-d'" class="gh-detail-row">
              <td :colspan="8">
                <div v-if="r.error" class="gh-error-box">{{ r.error }}</div>
                <table v-if="r.unplaced.length" class="gh-detail-table">
                  <thead>
                    <tr>
                      <th>Преподаватель</th><th>Дисциплина</th><th>Тип</th><th class="gh-num">Пары</th><th>Причина</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(u, i) in r.unplaced" :key="i">
                      <td class="gh-teacher">{{ u.teacher }}</td>
                      <td>{{ u.subject }}</td>
                      <td>{{ lessonTypeLabel[u.lessonType] ?? u.lessonType }}</td>
                      <td class="gh-num">
                        <span :class="['gh-pairs', u.placedPairs === 0 ? 'gh-none' : 'gh-partial']">
                          {{ u.placedPairs }} / {{ u.plannedPairs }}
                        </span>
                      </td>
                      <td class="gh-reason">{{ u.reason }}</td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.gh-container { background: white; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); display: flex; flex-direction: column; min-height: calc(100vh - 60px); font-family: sans-serif; }
.gh-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; }
.title-group { display: flex; align-items: center; gap: 12px; }
h2 { font-size: 20px; color: #1e293b; margin: 0; }
.gh-refresh { background: #f1f5f9; border: 1px solid #e2e8f0; color: #475569; padding: 8px 16px; border-radius: 8px; font-size: 13px; cursor: pointer; }
.gh-refresh:hover:not(:disabled) { background: #e2e8f0; }
.gh-refresh:disabled { opacity: 0.6; cursor: not-allowed; }

.filters-bar { display: flex; gap: 20px; padding: 0 24px 20px 24px; border-bottom: 1px solid #f1f5f9; }
.filter-group { display: flex; flex-direction: column; gap: 8px; flex: 1; max-width: 360px; }
label { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; }
.select-dropdown { height: 44px; padding: 0 12px; border: 1px solid #cbd5e1; border-radius: 8px; background: white; font-size: 14px; outline: none; cursor: pointer; }

.table-wrapper { overflow-x: auto; padding: 0 24px 24px 24px; }
.state-msg { padding: 40px 0; text-align: center; color: #64748b; font-size: 14px; }
.state-error { color: #dc2626; }

.gh-table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 16px; }
.gh-table thead th { color: #64748b; font-weight: 600; padding: 12px 10px; border-bottom: 2px solid #e2e8f0; text-align: left; font-size: 12px; white-space: nowrap; }
.gh-num { text-align: center !important; white-space: nowrap; }
.gh-arrow-th { width: 28px; }

.gh-row td { padding: 12px 10px; border-bottom: 1px solid #f1f5f9; vertical-align: middle; }
.gh-row.has-detail { cursor: pointer; }
.gh-row.has-detail:hover td { background: #f8fafc; }
.gh-arrow { color: #94a3b8; width: 28px; text-align: center; }
.gh-when { color: #475569; white-space: nowrap; }
.gh-inst { font-weight: 600; color: #1e293b; }
.gh-sem { color: #64748b; white-space: nowrap; }

.gh-badge { display: inline-flex; align-items: center; gap: 5px; padding: 3px 10px; border-radius: 9999px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.gh-ok { background: #dcfce7; color: #15803d; }
.gh-warn { background: #fef3c7; color: #b45309; }
.gh-err { background: #fee2e2; color: #b91c1c; }
.gh-unplaced { display: inline-block; min-width: 22px; padding: 2px 8px; border-radius: 9999px; background: #fef3c7; color: #b45309; font-weight: 700; }
.gh-dash { color: #cbd5e1; }

.gh-detail-row td { padding: 0 10px 14px 38px; background: #fafbfc; border-bottom: 1px solid #f1f5f9; }
.gh-error-box { background: #fef2f2; color: #b91c1c; border: 1px solid #fecaca; border-radius: 8px; padding: 10px 12px; font-size: 13px; margin: 10px 0; }
.gh-detail-table { width: 100%; border-collapse: collapse; font-size: 12.5px; margin: 6px 0; }
.gh-detail-table th { text-align: left; color: #94a3b8; font-weight: 600; padding: 6px 8px; border-bottom: 1px solid #e2e8f0; }
.gh-detail-table td { padding: 6px 8px; border-bottom: 1px solid #f1f5f9; color: #334155; vertical-align: top; }
.gh-teacher { font-weight: 600; white-space: nowrap; }
.gh-reason { color: #475569; }
.gh-pairs { display: inline-block; padding: 2px 8px; border-radius: 9999px; font-weight: 700; font-size: 11px; }
.gh-partial { background: #fef3c7; color: #b45309; }
.gh-none { background: #fee2e2; color: #b91c1c; }
</style>
