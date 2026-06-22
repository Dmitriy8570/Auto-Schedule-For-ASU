import { defineStore } from 'pinia'
import { ref } from 'vue'
import { lookups } from '../api/lookups'
import { calendar } from '../api/calendar'
import type { InstituteDto, BuildingDto, SemesterDto } from '../api/types'

// Кэш «глобальных» справочников (Pinia). Институты, семестры и корпуса одинаковы на всех вкладках
// и не зависят от выбранных фильтров — грузим их один раз за сессию, а не при каждом открытии
// вкладки. Каскадные списки (кафедры/преподаватели/группы под выбранным институтом) остаются
// динамическими и запрашиваются вкладками по мере необходимости.
export const useLookupsStore = defineStore('lookups', () => {
  const institutes = ref<InstituteDto[]>([])
  const semesters = ref<SemesterDto[]>([])
  const buildings = ref<BuildingDto[]>([])

  // Промисы загрузки — чтобы параллельные вызовы ensure* не дублировали запрос.
  let institutesP: Promise<InstituteDto[]> | null = null
  let semestersP: Promise<SemesterDto[]> | null = null
  let buildingsP: Promise<BuildingDto[]> | null = null

  async function ensureInstitutes(): Promise<InstituteDto[]> {
    if (institutes.value.length) return institutes.value
    institutesP ??= lookups.institutes().catch(() => [])
    institutes.value = await institutesP
    return institutes.value
  }

  async function ensureSemesters(): Promise<SemesterDto[]> {
    if (semesters.value.length) return semesters.value
    semestersP ??= calendar.semesters().catch(() => [])
    semesters.value = await semestersP
    return semesters.value
  }

  async function ensureBuildings(): Promise<BuildingDto[]> {
    if (buildings.value.length) return buildings.value
    buildingsP ??= lookups.buildings().catch(() => [])
    buildings.value = await buildingsP
    return buildings.value
  }

  // Сбросить кэш (например, после изменения справочников), чтобы следующий ensure* перезагрузил.
  function invalidate(): void {
    institutes.value = []; semesters.value = []; buildings.value = []
    institutesP = semestersP = buildingsP = null
  }

  return { institutes, semesters, buildings, ensureInstitutes, ensureSemesters, ensureBuildings, invalidate }
})
