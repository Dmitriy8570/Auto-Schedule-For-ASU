<script setup lang="ts">
// Выпадающий список с поиском по вводу (замена нативного <select> для длинных справочников:
// преподаватели, аудитории, учебные планы). v-model — строковый идентификатор выбранного пункта;
// пустая строка означает «ничего не выбрано» (показывается placeholder).
import { ref, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import { ChevronDown, Search, Check } from 'lucide-vue-next'

export interface SelectOption { value: string; label: string; sublabel?: string }

const props = withDefaults(defineProps<{
  modelValue: string
  options: SelectOption[]
  placeholder?: string
  searchPlaceholder?: string
  disabled?: boolean
  /** Если задано — первым пунктом показывается сброс выбора к '' с этой подписью (напр. «Все институты»). */
  clearLabel?: string
}>(), {
  placeholder: 'Выберите…',
  searchPlaceholder: 'Поиск…',
  disabled: false,
})

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>()

const open = ref(false)
const query = ref('')
const root = ref<HTMLElement | null>(null)
const searchInput = ref<HTMLInputElement | null>(null)
const activeIndex = ref(-1)

const selectedLabel = computed(() =>
  props.options.find(o => o.value === props.modelValue)?.label ?? '')

const filtered = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return props.options
  return props.options.filter(o =>
    o.label.toLowerCase().includes(q) || (o.sublabel?.toLowerCase().includes(q) ?? false))
})

function toggle() {
  if (props.disabled) return
  if (open.value) close(); else openDropdown()
}
function openDropdown() {
  open.value = true
  query.value = ''
  activeIndex.value = filtered.value.findIndex(o => o.value === props.modelValue)
  nextTick(() => searchInput.value?.focus())
}
function close() {
  open.value = false
  activeIndex.value = -1
}
function pick(value: string) {
  emit('update:modelValue', value)
  close()
}

function move(delta: number) {
  const n = filtered.value.length
  if (n === 0) { activeIndex.value = -1; return }
  activeIndex.value = (activeIndex.value + delta + n) % n
}
function onKeydown(e: KeyboardEvent) {
  if (!open.value) {
    if (e.key === 'Enter' || e.key === 'ArrowDown' || e.key === ' ') { e.preventDefault(); openDropdown() }
    return
  }
  if (e.key === 'Escape') { e.preventDefault(); close() }
  else if (e.key === 'ArrowDown') { e.preventDefault(); move(1) }
  else if (e.key === 'ArrowUp') { e.preventDefault(); move(-1) }
  else if (e.key === 'Enter') {
    e.preventDefault()
    const opt = filtered.value[activeIndex.value]
    if (opt) pick(opt.value)
  }
}

function onDocMouseDown(e: MouseEvent) {
  if (root.value && !root.value.contains(e.target as Node)) close()
}
onMounted(() => document.addEventListener('mousedown', onDocMouseDown))
onBeforeUnmount(() => document.removeEventListener('mousedown', onDocMouseDown))

// Если список опций изменился (каскадный фильтр), не оставляем подсветку за пределами списка.
watch(filtered, (list) => { if (activeIndex.value >= list.length) activeIndex.value = list.length - 1 })
</script>

<template>
  <div class="base-select" :class="{ disabled }" ref="root">
    <button
      type="button" class="bs-trigger"
      :class="{ 'is-open': open, 'is-placeholder': !selectedLabel }"
      :disabled="disabled" @click="toggle" @keydown="onKeydown"
    >
      <span class="bs-value">{{ selectedLabel || placeholder }}</span>
      <ChevronDown :size="16" class="bs-chevron" :class="{ 'is-open': open }" />
    </button>

    <div v-if="open" class="bs-panel">
      <div class="bs-search">
        <Search :size="15" class="bs-search-icon" />
        <input
          ref="searchInput" v-model="query" :placeholder="searchPlaceholder"
          class="bs-search-input" @keydown="onKeydown"
        />
      </div>
      <div class="bs-options">
        <button
          v-if="clearLabel" type="button" class="bs-option bs-clear"
          :class="{ selected: modelValue === '' }" @click="pick('')"
        >
          <span class="bs-option-label">{{ clearLabel }}</span>
          <Check v-if="modelValue === ''" :size="15" class="bs-check" />
        </button>
        <button
          v-for="(opt, i) in filtered" :key="opt.value" type="button" class="bs-option"
          :class="{ selected: opt.value === modelValue, active: i === activeIndex }"
          @click="pick(opt.value)" @mouseenter="activeIndex = i"
        >
          <span class="bs-option-text">
            <span class="bs-option-label">{{ opt.label }}</span>
            <span v-if="opt.sublabel" class="bs-option-sub">{{ opt.sublabel }}</span>
          </span>
          <Check v-if="opt.value === modelValue" :size="15" class="bs-check" />
        </button>
        <div v-if="filtered.length === 0" class="bs-empty">Ничего не найдено</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.base-select { position: relative; min-width: 180px; }
.base-select.disabled { opacity: 0.7; }

.bs-trigger {
  display: flex; align-items: center; justify-content: space-between; gap: 8px;
  width: 100%; padding: 10px 12px 10px 16px; border: 1px solid #cbd5e1; border-radius: 8px;
  background: white; color: #334155; font-size: 14px; font-family: inherit; cursor: pointer;
  outline: none; transition: border-color 0.2s; box-sizing: border-box;
}
.bs-trigger:hover:not(:disabled) { border-color: #94a3b8; }
.bs-trigger.is-open { border-color: #1a4d9c; }
.bs-trigger:disabled { background: #f8fafc; color: #94a3b8; border-color: #e2e8f0; cursor: not-allowed; }
.bs-trigger.is-placeholder .bs-value { color: #94a3b8; }
.bs-value { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; text-align: left; }
.bs-chevron { color: #94a3b8; flex-shrink: 0; transition: transform 0.2s; }
.bs-chevron.is-open { transform: rotate(180deg); }

.bs-panel {
  position: absolute; z-index: 50; top: calc(100% + 4px); left: 0; right: 0;
  background: white; border: 1px solid #e2e8f0; border-radius: 10px;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.12); overflow: hidden;
  display: flex; flex-direction: column; min-width: 220px;
}
.bs-search { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-bottom: 1px solid #f1f5f9; }
.bs-search-icon { color: #94a3b8; flex-shrink: 0; }
.bs-search-input { flex: 1; border: none; outline: none; font-size: 14px; color: #334155; font-family: inherit; background: transparent; }
.bs-search-input::placeholder { color: #94a3b8; }

.bs-options { max-height: 260px; overflow-y: auto; padding: 6px; }
.bs-options::-webkit-scrollbar { width: 6px; }
.bs-options::-webkit-scrollbar-thumb { background-color: #cbd5e1; border-radius: 4px; }

.bs-option {
  display: flex; align-items: center; justify-content: space-between; gap: 8px; width: 100%;
  padding: 9px 10px; border: none; border-radius: 7px; background: transparent; cursor: pointer;
  font-size: 14px; color: #334155; font-family: inherit; text-align: left;
}
.bs-option.active { background: #f1f5f9; }
.bs-option.selected { color: #1a4d9c; font-weight: 600; }
.bs-clear { color: #64748b; }
.bs-option-text { display: flex; flex-direction: column; gap: 1px; overflow: hidden; }
.bs-option-label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.bs-option-sub { font-size: 11px; color: #94a3b8; font-weight: 400; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.bs-check { color: #1a4d9c; flex-shrink: 0; }
.bs-empty { padding: 14px 10px; text-align: center; color: #94a3b8; font-size: 13px; }
</style>
