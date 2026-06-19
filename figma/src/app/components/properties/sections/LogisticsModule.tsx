import React, { useState } from 'react';
import {
  Plus, Trash2, MapPin, Route, ToggleLeft, ToggleRight,
  Split, Merge, Users, Building2, GraduationCap, Info
} from 'lucide-react';

// ─── Travel Rules ─────────────────────────────────────────────────────────────

export interface TravelRule {
  id: string;
  fromBuilding: string;
  toBuilding: string;
  travelMinutes: number;
  enforceBuffer: boolean;
}

const BUILDINGS = [
  'Главный корпус (А)',
  'Корпус Б',
  'Корпус В (Техн.)',
  'Корпус Г (Доп.)',
  'Корпус Д (Спорт)',
];

// Travel time matrix in minutes (default estimates)
const TRAVEL_MATRIX: Record<string, Record<string, number>> = {
  'Главный корпус (А)': { 'Корпус Б': 10, 'Корпус В (Техн.)': 25, 'Корпус Г (Доп.)': 40, 'Корпус Д (Спорт)': 15 },
  'Корпус Б': { 'Главный корпус (А)': 10, 'Корпус В (Техн.)': 20, 'Корпус Г (Доп.)': 35, 'Корпус Д (Спорт)': 20 },
  'Корпус В (Техн.)': { 'Главный корпус (А)': 25, 'Корпус Б': 20, 'Корпус Г (Доп.)': 20, 'Корпус Д (Спорт)': 30 },
  'Корпус Г (Доп.)': { 'Главный корпус (А)': 40, 'Корпус Б': 35, 'Корпус В (Техн.)': 20, 'Корпус Д (Спорт)': 45 },
  'Корпус Д (Спорт)': { 'Главный корпус (А)': 15, 'Корпус Б': 20, 'Корпус В (Техн.)': 30, 'Корпус Г (Доп.)': 45 },
};

// ─── Stream Configs ───────────────────────────────────────────────────────────

export interface StreamConfig {
  id: string;
  activityName: string;
  totalStudents: number;
  splitCount: number;
  simultaneousRooms: boolean;
  electiveGroups: string[];
  isElective: boolean;
  maxElectiveCapacity: number;
}

const GROUPS_PRESET = ['ИВТ-301', 'ИВТ-302', 'ПИ-201', 'КБ-101', 'ЭК-101', 'ИВТ-М101'];

// ─── Travel Rules Section ─────────────────────────────────────────────────────

function TravelRulesSection({
  rules,
  onChange,
}: {
  rules: TravelRule[];
  onChange: (r: TravelRule[]) => void;
}) {
  const add = () => {
    const from = BUILDINGS[0];
    const to = BUILDINGS[1];
    const estimated = TRAVEL_MATRIX[from]?.[to] ?? 15;
    onChange([
      ...rules,
      { id: Date.now().toString(), fromBuilding: from, toBuilding: to, travelMinutes: estimated, enforceBuffer: true },
    ]);
  };
  const remove = (id: string) => onChange(rules.filter(r => r.id !== id));
  const update = (id: string, patch: Partial<TravelRule>) =>
    onChange(rules.map(r => (r.id === id ? { ...r, ...patch } : r)));

  const handleBuildingChange = (id: string, field: 'fromBuilding' | 'toBuilding', val: string, rule: TravelRule) => {
    const from = field === 'fromBuilding' ? val : rule.fromBuilding;
    const to = field === 'toBuilding' ? val : rule.toBuilding;
    const estimated = TRAVEL_MATRIX[from]?.[to] ?? TRAVEL_MATRIX[to]?.[from] ?? 15;
    update(id, { [field]: val, travelMinutes: estimated });
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 mb-2">
        <Route className="w-4 h-4 text-[rgb(26,77,156)]" />
        <h4 className="text-xs font-semibold text-slate-700">Правила перемещения</h4>
        <span className="text-[10px] text-slate-400 ml-auto">Автоматически добавляет буфер к расписанию</span>
      </div>

      {rules.length === 0 && (
        <div className="text-center py-6 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
          <Route className="w-6 h-6 mx-auto mb-2 text-slate-300" />
          Нет правил перемещения
        </div>
      )}

      {rules.map(rule => {
        const tooLong = rule.travelMinutes >= 30;
        return (
          <div key={rule.id} className={`border rounded-xl p-3 space-y-2 ${tooLong && rule.enforceBuffer ? 'border-amber-200 bg-amber-50/40' : 'border-slate-200 bg-white'}`}>
            <div className="flex items-center gap-2">
              <Building2 className="w-3.5 h-3.5 text-slate-400 shrink-0" />
              <select
                value={rule.fromBuilding}
                onChange={e => handleBuildingChange(rule.id, 'fromBuilding', e.target.value, rule)}
                className="flex-1 text-xs border border-slate-200 rounded-md px-2 py-1.5 bg-white focus:outline-none"
              >
                {BUILDINGS.map(b => <option key={b} value={b}>{b}</option>)}
              </select>
              <Route className="w-3.5 h-3.5 text-slate-300 shrink-0" />
              <select
                value={rule.toBuilding}
                onChange={e => handleBuildingChange(rule.id, 'toBuilding', e.target.value, rule)}
                className="flex-1 text-xs border border-slate-200 rounded-md px-2 py-1.5 bg-white focus:outline-none"
              >
                {BUILDINGS.filter(b => b !== rule.fromBuilding).map(b => <option key={b} value={b}>{b}</option>)}
              </select>
              <button onClick={() => remove(rule.id)} className="p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded shrink-0">
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            </div>

            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 flex-1">
                <span className="text-[11px] text-slate-500">Время в пути:</span>
                <input
                  type="number"
                  min={5}
                  max={120}
                  step={5}
                  value={rule.travelMinutes}
                  onChange={e => update(rule.id, { travelMinutes: Number(e.target.value) })}
                  className="w-16 text-xs border border-slate-200 rounded-md px-2 py-1 text-center focus:outline-none"
                />
                <span className="text-[11px] text-slate-400">мин.</span>
                {tooLong && (
                  <span className="text-[10px] bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded-full border border-amber-200">
                    ≥30 мин!
                  </span>
                )}
              </div>

              <button
                onClick={() => update(rule.id, { enforceBuffer: !rule.enforceBuffer })}
                className={`flex items-center gap-1.5 text-[11px] px-2 py-1 rounded-lg border font-medium transition-all ${
                  rule.enforceBuffer
                    ? 'bg-[rgb(26,77,156)] text-white border-[rgb(26,77,156)]'
                    : 'bg-white text-slate-500 border-slate-200 hover:bg-slate-50'
                }`}
              >
                {rule.enforceBuffer ? <ToggleRight className="w-3.5 h-3.5" /> : <ToggleLeft className="w-3.5 h-3.5" />}
                {rule.enforceBuffer ? 'Буфер вкл.' : 'Игнор.'}
              </button>
            </div>
          </div>
        );
      })}

      <button
        onClick={add}
        className="w-full flex items-center justify-center gap-2 border-2 border-dashed border-slate-200 text-slate-500 rounded-xl py-2 text-xs font-medium hover:border-[rgb(26,77,156)]/30 hover:text-[rgb(26,77,156)] hover:bg-[rgb(26,77,156)]/5 transition-all"
      >
        <Plus className="w-3.5 h-3.5" />
        Добавить маршрут
      </button>
    </div>
  );
}

// ─── Stream Split Section ─────────────────────────────────────────────────────

function StreamSplitSection({
  streams,
  onChange,
}: {
  streams: StreamConfig[];
  onChange: (s: StreamConfig[]) => void;
}) {
  const [groupInputs, setGroupInputs] = useState<Record<string, string>>({});

  const add = () =>
    onChange([
      ...streams,
      {
        id: Date.now().toString(),
        activityName: '',
        totalStudents: 100,
        splitCount: 4,
        simultaneousRooms: true,
        electiveGroups: [],
        isElective: false,
        maxElectiveCapacity: 30,
      },
    ]);

  const remove = (id: string) => onChange(streams.filter(s => s.id !== id));
  const update = (id: string, patch: Partial<StreamConfig>) =>
    onChange(streams.map(s => (s.id === id ? { ...s, ...patch } : s)));

  const addGroup = (id: string, stream: StreamConfig) => {
    const g = (groupInputs[id] ?? '').trim();
    if (!g || stream.electiveGroups.includes(g)) return;
    update(id, { electiveGroups: [...stream.electiveGroups, g] });
    setGroupInputs(prev => ({ ...prev, [id]: '' }));
  };

  return (
    <div className="space-y-3 mt-4 pt-4 border-t border-slate-100">
      <div className="flex items-center gap-2 mb-2">
        <Split className="w-4 h-4 text-teal-600" />
        <h4 className="text-xs font-semibold text-slate-700">Потоки: разбивка и объединение</h4>
      </div>

      {streams.length === 0 && (
        <div className="text-center py-6 border border-dashed border-slate-200 rounded-xl text-sm text-slate-400">
          <Split className="w-6 h-6 mx-auto mb-2 text-slate-300" />
          Нет конфигураций потоков
        </div>
      )}

      {streams.map(s => {
        const perGroup = s.splitCount > 0 ? Math.ceil(s.totalStudents / s.splitCount) : s.totalStudents;
        return (
          <div key={s.id} className="border border-slate-200 rounded-xl bg-white overflow-hidden">
            {/* Header */}
            <div className="flex items-center gap-2 px-3 py-2 bg-teal-50/60 border-b border-teal-100">
              {s.isElective ? (
                <GraduationCap className="w-3.5 h-3.5 text-teal-600" />
              ) : (
                <Split className="w-3.5 h-3.5 text-teal-600" />
              )}
              <input
                value={s.activityName}
                onChange={e => update(s.id, { activityName: e.target.value })}
                placeholder="Название потока / занятия…"
                className="flex-1 text-xs bg-transparent border-none outline-none text-slate-700 placeholder-slate-400"
              />
              <button
                onClick={() => update(s.id, { isElective: !s.isElective })}
                className={`text-[10px] px-2 py-0.5 rounded-full border font-medium transition-all ${
                  s.isElective ? 'bg-orange-100 border-orange-200 text-orange-700' : 'bg-teal-100 border-teal-200 text-teal-700'
                }`}
              >
                {s.isElective ? 'По выбору' : 'Поток'}
              </button>
              <button onClick={() => remove(s.id)} className="p-1 text-slate-400 hover:text-red-500 rounded">
                <Trash2 className="w-3 h-3" />
              </button>
            </div>

            <div className="p-3 space-y-3">
              {!s.isElective ? (
                <>
                  {/* Stream visualiser */}
                  <div className="flex items-center gap-2">
                    {/* Big group */}
                    <div className="flex flex-col items-center">
                      <div className="w-14 h-8 rounded-lg bg-teal-100 border border-teal-300 flex items-center justify-center">
                        <span className="text-[10px] font-bold text-teal-700">{s.totalStudents} чел.</span>
                      </div>
                      <span className="text-[9px] text-slate-400 mt-0.5">Поток</span>
                    </div>
                    <Split className="w-5 h-5 text-slate-300 rotate-90" />
                    {/* Sub-groups */}
                    <div className="flex gap-1 flex-wrap">
                      {Array.from({ length: Math.min(s.splitCount, 6) }, (_, i) => (
                        <div key={i} className="flex flex-col items-center">
                          <div className="w-10 h-7 rounded-md bg-slate-100 border border-slate-300 flex items-center justify-center">
                            <span className="text-[9px] font-medium text-slate-600">{perGroup}</span>
                          </div>
                          <span className="text-[8px] text-slate-400">Гр.{i + 1}</span>
                        </div>
                      ))}
                      {s.splitCount > 6 && (
                        <div className="flex flex-col items-center justify-center w-10">
                          <span className="text-[9px] text-slate-400">+{s.splitCount - 6}</span>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="grid grid-cols-3 gap-2">
                    <div>
                      <label className="block text-[10px] text-slate-400 mb-1">Студентов</label>
                      <input
                        type="number"
                        min={10}
                        max={500}
                        step={5}
                        value={s.totalStudents}
                        onChange={e => update(s.id, { totalStudents: Number(e.target.value) })}
                        className="w-full text-xs border border-slate-200 rounded-md px-2 py-1.5 text-center focus:outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-[10px] text-slate-400 mb-1">Подгрупп</label>
                      <input
                        type="number"
                        min={1}
                        max={20}
                        value={s.splitCount}
                        onChange={e => update(s.id, { splitCount: Number(e.target.value) })}
                        className="w-full text-xs border border-slate-200 rounded-md px-2 py-1.5 text-center focus:outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-[10px] text-slate-400 mb-1">На группу</label>
                      <div className="text-xs border border-slate-100 rounded-md px-2 py-1.5 text-center bg-slate-50 text-slate-600 font-medium">
                        {perGroup}
                      </div>
                    </div>
                  </div>

                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={s.simultaneousRooms}
                      onChange={e => update(s.id, { simultaneousRooms: e.target.checked })}
                      className="w-3.5 h-3.5"
                    />
                    <span className="text-[11px] text-slate-600">Все подгруппы — одновременно, в разных аудиториях</span>
                  </label>
                </>
              ) : (
                /* Elective config */
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] text-slate-600">Макс. вместимость:</span>
                    <input
                      type="number"
                      min={10}
                      max={200}
                      value={s.maxElectiveCapacity}
                      onChange={e => update(s.id, { maxElectiveCapacity: Number(e.target.value) })}
                      className="w-16 text-xs border border-slate-200 rounded-md px-2 py-1 text-center focus:outline-none"
                    />
                    <span className="text-[11px] text-slate-400">чел.</span>
                  </div>
                  <p className="text-[10px] text-slate-400 flex items-start gap-1">
                    <Info className="w-3 h-3 shrink-0 mt-0.5" />
                    Студенты из перечисленных групп могут записаться на этот предмет по выбору.
                  </p>
                  <div className="flex flex-wrap gap-1.5">
                    {s.electiveGroups.map(g => (
                      <span key={g} className="inline-flex items-center gap-1 bg-orange-50 text-orange-700 border border-orange-200 text-[11px] px-2 py-0.5 rounded-full">
                        {g}
                        <button onClick={() => update(s.id, { electiveGroups: s.electiveGroups.filter(x => x !== g) })}>
                          <Plus className="w-3 h-3 rotate-45 hover:text-red-500" />
                        </button>
                      </span>
                    ))}
                  </div>
                  <div className="flex gap-1.5">
                    <select
                      value={groupInputs[s.id] ?? ''}
                      onChange={e => setGroupInputs(prev => ({ ...prev, [s.id]: e.target.value }))}
                      className="flex-1 text-xs border border-slate-200 rounded-md px-2 py-1.5 bg-white focus:outline-none"
                    >
                      <option value="">Добавить группу…</option>
                      {GROUPS_PRESET.filter(g => !s.electiveGroups.includes(g)).map(g => (
                        <option key={g} value={g}>{g}</option>
                      ))}
                    </select>
                    <button
                      onClick={() => addGroup(s.id, s)}
                      className="px-3 py-1.5 bg-orange-500 text-white rounded-md text-xs hover:bg-orange-600"
                    >
                      <Plus className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        );
      })}

      <button
        onClick={add}
        className="w-full flex items-center justify-center gap-2 border-2 border-dashed border-teal-200 text-teal-600 rounded-xl py-2.5 text-xs font-medium hover:bg-teal-50 hover:border-teal-300 transition-all"
      >
        <Plus className="w-3.5 h-3.5" />
        Добавить поток / курс по выбору
      </button>
    </div>
  );
}

// ─── Main Export ──────────────────────────────────────────────────────────────

interface Props {
  travelRules: TravelRule[];
  streams: StreamConfig[];
  onTravelChange: (r: TravelRule[]) => void;
  onStreamsChange: (s: StreamConfig[]) => void;
}

export function LogisticsModule({ travelRules, streams, onTravelChange, onStreamsChange }: Props) {
  return (
    <div>
      <TravelRulesSection rules={travelRules} onChange={onTravelChange} />
      <StreamSplitSection streams={streams} onChange={onStreamsChange} />
    </div>
  );
}
