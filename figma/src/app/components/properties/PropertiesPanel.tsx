import React, { useState, useCallback, useRef, useEffect } from 'react';
import {
  ChevronDown, ChevronRight, User, MapPin, BookOpen, Users,
  Clock, Wrench, GitBranch, AlertTriangle, CheckCircle, XCircle,
  Plus, X, Trash2, Info, Lock, Unlock, Calendar, Building2,
  ZapOff, Zap, ArrowLeftRight, AlignJustify, Timer, Ban,
  Globe, SlidersHorizontal, Layers, Route, Briefcase,
  RefreshCw, BarChart3, Settings2,
} from 'lucide-react';
import { DistributionRules } from './sections/DistributionRules';
import type { DistributionRule } from './sections/DistributionRules';
import { RelationshipBuilder } from './sections/RelationshipBuilder';
import type { ActivityLink } from './sections/RelationshipBuilder';
import { LogisticsModule } from './sections/LogisticsModule';
import type { TravelRule, StreamConfig } from './sections/LogisticsModule';
import { ContractModule } from './sections/ContractModule';
import type { ContractConfig } from './sections/ContractModule';
import { ConflictEngine, computeConflicts } from './sections/ConflictEngine';
import type { ConflictItem } from './sections/ConflictEngine';
import { DEFAULT_CONTRACT } from './sections/ContractModule';

// ─── Base Types ───────────────────────────────────────────────────────────────

export type CellState = 'required' | 'preferred' | 'neutral' | 'discouraged' | 'prohibited';
export type EntityType = 'teacher' | 'room' | 'group' | 'subject';
export type ConditionType = 'consecutive_with' | 'same_day_as' | 'min_gap' | 'max_gap' | 'not_same_day' | 'must_before' | 'must_after';

export interface ConstraintRule {
  id: string;
  conditionType: ConditionType;
  targetName: string;
  gapValue: number;
  constraintType: 'hard' | 'soft';
  weight: number;
}

export interface EntityConstraints {
  name: string;
  code: string;
  entityType: EntityType;
  // Basic tab
  timeGrid: Record<string, CellState>;
  maxGapsPerDay: number;
  maxHoursPerDay: number;
  requiredEquipment: string[];
  roomCapacity: number;
  buildingPreference: string;
  rules: ConstraintRule[];           // legacy simple rules (kept for compat)
  // Distribution tab
  distributionRules: DistributionRule[];
  activityLinks: ActivityLink[];
  // Logistics tab
  travelRules: TravelRule[];
  streams: StreamConfig[];
  // Contract tab
  contract: ContractConfig;
  // Scope
  applyScope: 'local' | 'department' | 'global';
}

// ─── Constants ────────────────────────────────────────────────────────────────

const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
const PERIODS = [1, 2, 3, 4, 5, 6, 7, 8];
const PERIOD_TIMES: Record<number, string> = {
  1: '08:00', 2: '09:40', 3: '11:30', 4: '13:10',
  5: '15:00', 6: '16:40', 7: '18:20', 8: '20:00',
};
const CELL_CYCLE: CellState[] = ['neutral', 'preferred', 'required', 'discouraged', 'prohibited'];
const CELL_STYLES: Record<CellState, { bg: string; border: string; label: string; dot: string }> = {
  required:    { bg: 'bg-green-500',  border: 'border-green-600',  label: 'Обязат.',     dot: 'bg-green-500' },
  preferred:   { bg: 'bg-green-200',  border: 'border-green-400',  label: 'Предпочт.',   dot: 'bg-green-300' },
  neutral:     { bg: 'bg-slate-50',   border: 'border-slate-200',  label: 'Нейтр.',      dot: 'bg-slate-300' },
  discouraged: { bg: 'bg-yellow-200', border: 'border-yellow-400', label: 'Нежел.',      dot: 'bg-yellow-400' },
  prohibited:  { bg: 'bg-red-400',    border: 'border-red-500',    label: 'Запрещено',   dot: 'bg-red-500' },
};
const BUILDINGS = ['Без предпочтений', 'Главный корпус (А)', 'Корпус Б', 'Корпус В (Техн.)', 'Корпус Г', 'Корпус Д (Спорт)'];
const EQUIPMENT_SUGGESTIONS = ['Проектор', 'Интерактивная доска', 'iMac', 'Лабораторный стенд', 'Осциллограф', '3D-принтер', 'Микроскоп', 'Графический планшет', 'Звуковое оборудование', 'Видеокамера', 'Химический вытяжной шкаф', 'Паяльная станция'];
const CONDITION_OPTIONS: { value: ConditionType; label: string }[] = [
  { value: 'consecutive_with', label: 'Следует подряд с…' },
  { value: 'same_day_as',      label: 'В тот же день' },
  { value: 'min_gap',          label: 'Мин. разрыв дней' },
  { value: 'max_gap',          label: 'Макс. разрыв дней' },
  { value: 'not_same_day',     label: 'НЕ в тот же день' },
  { value: 'must_before',      label: 'Должно быть раньше' },
  { value: 'must_after',       label: 'Должно быть позже' },
];

// ─── Tabs definition ──────────────────────────────────────────────────────────

type TabId = 'basic' | 'distribution' | 'logistics' | 'contract' | 'conflicts';

interface TabDef {
  id: TabId;
  label: string;
  icon: React.FC<{ className?: string }>;
  badgeFn?: (c: EntityConstraints, conflicts: ConflictItem[]) => number | undefined;
  hidden?: (c: EntityConstraints) => boolean;
}

const TABS: TabDef[] = [
  { id: 'basic',        label: 'Основное',      icon: Settings2 },
  { id: 'distribution', label: 'Правила',       icon: BarChart3,
    badgeFn: (c) => (c.distributionRules.length + c.activityLinks.length) || undefined },
  { id: 'logistics',    label: 'Логистика',     icon: Route,
    badgeFn: (c) => (c.travelRules.length + c.streams.length) || undefined },
  { id: 'contract',     label: 'Контракт',      icon: Briefcase,
    hidden: (c) => c.entityType !== 'teacher' },
  { id: 'conflicts',    label: 'Конфликты',     icon: AlertTriangle,
    badgeFn: (_c, conf) => conf.filter(x => x.severity !== 'ok' && !x.dismissed).length || undefined },
];

const TYPE_ICONS: Record<EntityType, React.FC<{ className?: string }>> = {
  teacher: User, room: MapPin, group: Users, subject: BookOpen,
};
const TYPE_LABELS: Record<EntityType, string> = {
  teacher: 'Преподаватель', room: 'Аудитория', group: 'Группа', subject: 'Дисциплина',
};

// ─── Default factory ──────────────────────────────────────────────────────────

function makeDefaultConstraints(entity: { id: string; name: string; type: EntityType }): EntityConstraints {
  const grid: Record<string, CellState> = {};
  DAYS.forEach((_, di) => {
    PERIODS.forEach(p => { grid[`${di}-${p}`] = 'neutral'; });
  });
  if (entity.type === 'teacher') {
    [5, 6, 7, 8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
    DAYS.forEach((_, di) => {
      if (di < 5) { grid[`${di}-1`] = 'preferred'; grid[`${di}-2`] = 'preferred'; }
    });
  }
  return {
    name: entity.name,
    code: entity.type === 'teacher' ? 'PROF' : entity.type === 'room' ? 'AUD' : 'GRP',
    entityType: entity.type,
    timeGrid: grid,
    maxGapsPerDay: 2,
    maxHoursPerDay: entity.type === 'teacher' ? 8 : 12,
    requiredEquipment: entity.type === 'room' ? ['Проектор'] : [],
    roomCapacity: entity.type === 'room' ? 30 : 0,
    buildingPreference: 'Без предпочтений',
    rules: [],
    distributionRules: [
      { id: 'd1', pattern: 'min_days_between', value: 2, weight: 65, scope: 'local' },
    ],
    activityLinks: [],
    travelRules: entity.type === 'teacher' ? [
      { id: 'tr1', fromBuilding: 'Главный корпус (А)', toBuilding: 'Корпус В (Техн.)', travelMinutes: 25, enforceBuffer: true },
    ] : [],
    streams: [],
    contract: { ...DEFAULT_CONTRACT },
    applyScope: 'local',
  };
}

// ─── Section accordion wrapper ────────────────────────────────────────────────

function Section({
  title, icon: Icon, defaultOpen = true, children,
}: {
  title: string;
  icon: React.FC<{ className?: string }>;
  defaultOpen?: boolean;
  children: React.ReactNode;
}) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border border-slate-200 rounded-xl overflow-hidden shadow-sm">
      <button
        onClick={() => setOpen(o => !o)}
        className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${open ? 'bg-slate-50 border-b border-slate-200' : 'bg-white hover:bg-slate-50'}`}
      >
        <span className="p-1.5 rounded-lg bg-[rgb(26,77,156)]/10">
          <Icon className="w-4 h-4 text-[rgb(26,77,156)]" />
        </span>
        <span className="flex-1 text-sm font-semibold text-slate-700">{title}</span>
        {open ? <ChevronDown className="w-4 h-4 text-slate-400" /> : <ChevronRight className="w-4 h-4 text-slate-400" />}
      </button>
      {open && <div className="bg-white p-4">{children}</div>}
    </div>
  );
}

// ─── Time Grid ────────────────────────────────────────────────────────────────

function TimeGrid({ grid, onChange }: { grid: Record<string, CellState>; onChange: (g: Record<string, CellState>) => void }) {
  const [isDragging, setIsDragging] = useState(false);
  const [dragState, setDragState] = useState<CellState>('neutral');

  const cycleState = useCallback((key: string): CellState => {
    const cur = grid[key] ?? 'neutral';
    return CELL_CYCLE[(CELL_CYCLE.indexOf(cur) + 1) % CELL_CYCLE.length];
  }, [grid]);

  useEffect(() => {
    const up = () => setIsDragging(false);
    window.addEventListener('mouseup', up);
    return () => window.removeEventListener('mouseup', up);
  }, []);

  const fillAll = (s: CellState) => {
    const g: Record<string, CellState> = {};
    DAYS.forEach((_, di) => { PERIODS.forEach(p => { g[`${di}-${p}`] = s; }); });
    onChange(g);
  };

  return (
    <div className="select-none">
      <div className="flex flex-wrap gap-2 mb-3">
        {(Object.entries(CELL_STYLES) as [CellState, typeof CELL_STYLES[CellState]][]).map(([state, s]) => (
          <div key={state} className="flex items-center gap-1.5">
            <span className={`w-2.5 h-2.5 rounded-sm ${s.bg} border ${s.border}`} />
            <span className="text-[11px] text-slate-500">{s.label}</span>
          </div>
        ))}
      </div>
      <div className="flex gap-1.5 mb-3 flex-wrap">
        <span className="text-[11px] text-slate-400 self-center mr-1">Заполнить:</span>
        {(['neutral', 'preferred', 'required', 'prohibited'] as CellState[]).map(s => (
          <button key={s} onClick={() => fillAll(s)} className={`text-[10px] px-2 py-1 rounded border ${CELL_STYLES[s].bg} ${CELL_STYLES[s].border} text-slate-700 hover:opacity-80`}>
            {CELL_STYLES[s].label}
          </button>
        ))}
      </div>
      <div className="overflow-x-auto">
        <div className="min-w-max" onMouseLeave={() => setIsDragging(false)}>
          <div className="flex mb-1">
            <div className="w-16 shrink-0" />
            {DAYS.map(d => <div key={d} className="w-9 text-center text-[11px] font-semibold text-slate-500">{d}</div>)}
          </div>
          {PERIODS.map(p => (
            <div key={p} className="flex items-center mb-0.5">
              <div className="w-16 shrink-0 pr-2 text-right">
                <span className="text-[10px] font-medium text-slate-500">{p}п</span>
                <span className="text-[9px] text-slate-400 block">{PERIOD_TIMES[p]}</span>
              </div>
              {DAYS.map((_, di) => {
                const key = `${di}-${p}`;
                const state = grid[key] ?? 'neutral';
                const { bg, border } = CELL_STYLES[state];
                return (
                  <div
                    key={key}
                    onMouseDown={() => {
                      const next = cycleState(key);
                      setDragState(next);
                      setIsDragging(true);
                      onChange({ ...grid, [key]: next });
                    }}
                    onMouseEnter={() => { if (isDragging) onChange({ ...grid, [key]: dragState }); }}
                    title={CELL_STYLES[state].label}
                    className={`w-9 h-8 mx-0.5 rounded cursor-pointer border-2 transition-all duration-100 hover:scale-105 hover:shadow-md ${bg} ${border}`}
                  />
                );
              })}
            </div>
          ))}
        </div>
      </div>
      <div className="mt-3 flex gap-3 flex-wrap">
        {(['required', 'preferred', 'prohibited'] as CellState[]).map(s => {
          const count = Object.values(grid).filter(v => v === s).length;
          return (
            <div key={s} className="flex items-center gap-1.5">
              <span className={`w-2 h-2 rounded-full ${CELL_STYLES[s].dot}`} />
              <span className="text-[11px] text-slate-500">{CELL_STYLES[s].label}: <b>{count}</b></span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── Equipment Tags ───────────────────────────────────────────────────────────

function EquipmentInput({ tags, onChange }: { tags: string[]; onChange: (t: string[]) => void }) {
  const [input, setInput] = useState('');
  const [show, setShow] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const filtered = EQUIPMENT_SUGGESTIONS.filter(s => !tags.includes(s) && s.toLowerCase().includes(input.toLowerCase()));

  useEffect(() => {
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setShow(false); };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, []);

  const add = (t: string) => { const v = t.trim(); if (v && !tags.includes(v)) onChange([...tags, v]); setInput(''); setShow(false); };

  return (
    <div ref={ref} className="relative">
      <div className="flex flex-wrap gap-1.5 p-2 min-h-[2.75rem] border border-slate-200 rounded-lg bg-white focus-within:ring-2 focus-within:ring-[rgb(26,77,156)]/30">
        {tags.map(tag => (
          <span key={tag} className="inline-flex items-center gap-1 bg-[rgb(26,77,156)]/10 text-[rgb(26,77,156)] text-xs px-2 py-0.5 rounded-full border border-[rgb(26,77,156)]/20">
            {tag}
            <button onClick={() => onChange(tags.filter(t => t !== tag))} className="hover:text-red-500"><X className="w-3 h-3" /></button>
          </span>
        ))}
        <input
          value={input}
          onChange={e => { setInput(e.target.value); setShow(true); }}
          onFocus={() => setShow(true)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add(input); } if (e.key === 'Backspace' && !input && tags.length) onChange(tags.slice(0, -1)); }}
          placeholder={tags.length === 0 ? 'Добавить оборудование…' : ''}
          className="flex-1 min-w-[8rem] outline-none text-sm bg-transparent placeholder-slate-400"
        />
      </div>
      {show && filtered.length > 0 && (
        <div className="absolute z-50 left-0 right-0 mt-1 bg-white border border-slate-200 rounded-lg shadow-xl overflow-hidden">
          {filtered.slice(0, 6).map(s => (
            <button key={s} onMouseDown={e => { e.preventDefault(); add(s); }} className="w-full text-left px-3 py-2 text-sm text-slate-700 hover:bg-[rgb(26,77,156)]/5 hover:text-[rgb(26,77,156)] flex items-center gap-2">
              <Wrench className="w-3.5 h-3.5 text-slate-400" />{s}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Main Panel ───────────────────────────────────────────────────────────────

interface PropertiesPanelProps {
  entity: { id: string; name: string; type: EntityType; subLabel?: string };
  onSave?: (constraints: EntityConstraints) => void;
}

export function PropertiesPanel({ entity, onSave }: PropertiesPanelProps) {
  const [c, setC] = useState<EntityConstraints>(() => makeDefaultConstraints(entity));
  const [activeTab, setActiveTab] = useState<TabId>('basic');
  const [saved, setSaved] = useState(false);
  const [conflicts, setConflicts] = useState<ConflictItem[]>([]);
  const [lastChecked, setLastChecked] = useState<Date | undefined>(undefined);
  const [dismissedIds, setDismissedIds] = useState<Set<string>>(new Set());

  useEffect(() => { setC(makeDefaultConstraints(entity)); setSaved(false); setConflicts([]); setDismissedIds(new Set()); }, [entity.id]);

  const update = useCallback(<K extends keyof EntityConstraints>(key: K, val: EntityConstraints[K]) => {
    setC(prev => ({ ...prev, [key]: val }));
    setSaved(false);
  }, []);

  const runConflictCheck = useCallback(() => {
    const raw = computeConflicts({
      timeGrid: c.timeGrid,
      maxHoursPerDay: c.maxHoursPerDay,
      distributionRules: c.distributionRules,
      activityLinks: c.activityLinks,
      travelRules: c.travelRules,
      streams: c.streams,
      contract: c.contract,
      entityType: c.entityType,
    });
    setConflicts(raw.map(r => ({ ...r, dismissed: dismissedIds.has(r.id) })));
    setLastChecked(new Date());
  }, [c, dismissedIds]);

  useEffect(() => { runConflictCheck(); }, [c]);

  const visibleConflicts = conflicts.filter(x => !dismissedIds.has(x.id));
  const conflictBadge = visibleConflicts.filter(x => x.severity !== 'ok').length;

  const handleSave = () => { onSave?.(c); setSaved(true); setTimeout(() => setSaved(false), 2000); };
  const handleDismiss = (id: string) => { setDismissedIds(prev => new Set(prev).add(id)); };

  const TypeIcon = TYPE_ICONS[entity.type];
  const visibleTabs = TABS.filter(t => !t.hidden?.(c));

  // ── Scope toggle chips ───
  const scopeOptions: { val: EntityConstraints['applyScope']; label: string; color: string }[] = [
    { val: 'local',      label: 'Объект',  color: c.applyScope === 'local'      ? 'bg-[rgb(26,77,156)] text-white border-[rgb(26,77,156)]' : 'bg-white text-slate-500 border-slate-200' },
    { val: 'department', label: 'Кафедра', color: c.applyScope === 'department' ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white text-slate-500 border-slate-200' },
    { val: 'global',     label: 'Весь вуз', color: c.applyScope === 'global'    ? 'bg-purple-600 text-white border-purple-600' : 'bg-white text-slate-500 border-slate-200' },
  ];

  return (
    <div className="flex flex-col h-full bg-slate-50 overflow-hidden">

      {/* ── Panel Header ── */}
      <div className="bg-white border-b border-slate-200 px-5 py-3.5 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-[rgb(26,77,156)] flex items-center justify-center shrink-0">
            <TypeIcon className="w-5 h-5 text-white" />
          </div>
          <div className="flex-1 min-w-0">
            <h2 className="text-sm font-bold text-slate-800 truncate">{entity.name}</h2>
            <p className="text-[11px] text-slate-400">{TYPE_LABELS[entity.type]}{entity.subLabel ? ` · ${entity.subLabel}` : ''}</p>
          </div>

          {/* Scope selector */}
          <div className="flex items-center gap-1 bg-slate-100 rounded-lg p-0.5">
            {scopeOptions.map(s => (
              <button
                key={s.val}
                onClick={() => update('applyScope', s.val)}
                className={`text-[10px] px-2.5 py-1.5 rounded-md border font-semibold transition-all ${s.color}`}
              >
                {s.label}
              </button>
            ))}
          </div>

          {/* Conflict indicator */}
          {conflictBadge > 0 && (
            <span className={`flex items-center gap-1 text-[11px] px-2 py-1 rounded-full border font-semibold ${
              visibleConflicts.some(x => x.severity === 'critical')
                ? 'bg-red-100 text-red-700 border-red-200'
                : 'bg-amber-100 text-amber-700 border-amber-200'
            }`}>
              <AlertTriangle className="w-3 h-3" />
              {conflictBadge}
            </span>
          )}

          <button
            onClick={handleSave}
            className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg transition-all ${
              saved
                ? 'bg-green-100 text-green-700 border border-green-200'
                : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'
            }`}
          >
            {saved ? <><CheckCircle className="w-3.5 h-3.5" /> Сохранено</> : 'Сохранить'}
          </button>
        </div>
      </div>

      {/* ── Tab bar ── */}
      <div className="flex bg-white border-b border-slate-200 shrink-0 overflow-x-auto">
        {visibleTabs.map(tab => {
          const badge = tab.badgeFn?.(c, conflicts.map(x => ({ ...x, dismissed: dismissedIds.has(x.id) })));
          const TabIcon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-3 text-xs font-semibold whitespace-nowrap border-b-2 transition-all ${
                isActive
                  ? 'border-[rgb(26,77,156)] text-[rgb(26,77,156)] bg-blue-50/40'
                  : 'border-transparent text-slate-500 hover:text-slate-700 hover:bg-slate-50'
              }`}
            >
              <TabIcon className="w-3.5 h-3.5" />
              {tab.label}
              {badge !== undefined && badge > 0 && (
                <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-bold ml-0.5 ${
                  tab.id === 'conflicts'
                    ? (visibleConflicts.some(x => x.severity === 'critical') ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700')
                    : 'bg-[rgb(26,77,156)]/15 text-[rgb(26,77,156)]'
                }`}>
                  {badge}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* ── Tab content ── */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3">

        {/* ════ TAB: BASIC ════ */}
        {activeTab === 'basic' && (
          <>
            {/* General Info */}
            <Section title="Общая информация" icon={Info}>
              <div className="grid grid-cols-2 gap-3 mb-3">
                <div>
                  <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1">Название</label>
                  <input value={c.name} onChange={e => update('name', e.target.value)} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30 bg-white" />
                </div>
                <div>
                  <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1">Код</label>
                  <input value={c.code} onChange={e => update('code', e.target.value)} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30 bg-white font-mono" />
                </div>
              </div>
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1">Тип объекта</label>
                <div className="flex gap-2">
                  {(['teacher', 'room', 'group', 'subject'] as EntityType[]).map(t => {
                    const TIcon = TYPE_ICONS[t];
                    return (
                      <button key={t} onClick={() => update('entityType', t)} className={`flex-1 flex flex-col items-center gap-1 py-2.5 rounded-lg border text-[11px] font-medium transition-all ${c.entityType === t ? 'bg-[rgb(26,77,156)] border-[rgb(26,77,156)] text-white shadow-md' : 'bg-white border-slate-200 text-slate-500 hover:border-[rgb(26,77,156)]/40'}`}>
                        <TIcon className="w-4 h-4" />
                        {TYPE_LABELS[t]}
                      </button>
                    );
                  })}
                </div>
              </div>
            </Section>

            {/* Time Availability */}
            <Section title="Доступность по времени" icon={Clock}>
              <TimeGrid grid={c.timeGrid} onChange={g => update('timeGrid', g)} />
              <div className="grid grid-cols-2 gap-4 mt-4 pt-4 border-t border-slate-100">
                {[
                  { key: 'maxGapsPerDay' as const, label: 'Макс. «окон» в день', unit: '«окон»', min: 0, max: 8 },
                  { key: 'maxHoursPerDay' as const, label: 'Макс. часов в день', unit: 'ч/день', min: 1, max: 16 },
                ].map(({ key, label, unit, min, max }) => (
                  <div key={key}>
                    <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-2">{label}</label>
                    <div className="flex items-center gap-2">
                      {[{ op: -1, sym: '−' }, { op: 1, sym: '+' }].map(({ op, sym }, i) => (
                        <button key={i} onClick={() => { const v = c[key] + op; if (v >= min && v <= max) update(key, v); }} className="w-7 h-7 rounded-md bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-700 transition-colors font-bold">
                          {sym}
                        </button>
                      ))}
                      <span className="w-8 text-center text-sm font-semibold text-slate-700">{c[key]}</span>
                      <span className="text-[11px] text-slate-400">{unit}</span>
                    </div>
                  </div>
                ))}
              </div>
            </Section>

            {/* Equipment */}
            <Section title="Ресурсы и оборудование" icon={Wrench} defaultOpen={false}>
              <div className="space-y-4">
                <div>
                  <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">
                    {c.entityType === 'room' ? 'Имеющееся оборудование' : 'Требуемое оборудование'}
                  </label>
                  <EquipmentInput tags={c.requiredEquipment} onChange={t => update('requiredEquipment', t)} />
                </div>
                {(c.entityType === 'room' || c.entityType === 'group') && (
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">
                      {c.entityType === 'room' ? 'Вместимость (мест)' : 'Размер группы (чел.)'}
                    </label>
                    <div className="flex items-center gap-3">
                      <input
                        type="range" min={5} max={500} step={5} value={c.roomCapacity || 30}
                        onChange={e => update('roomCapacity', Number(e.target.value))}
                        className="flex-1 h-2 rounded-full appearance-none cursor-pointer"
                        style={{ background: `linear-gradient(to right, rgb(26,77,156) 0%, rgb(26,77,156) ${((c.roomCapacity - 5) / 495) * 100}%, #e2e8f0 ${((c.roomCapacity - 5) / 495) * 100}%, #e2e8f0 100%)` }}
                      />
                      <span className="text-sm font-semibold text-slate-700 w-12 text-right">{c.roomCapacity || 30}</span>
                    </div>
                  </div>
                )}
                <div>
                  <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Предпочтение корпуса</label>
                  <select value={c.buildingPreference} onChange={e => update('buildingPreference', e.target.value)} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
                    {BUILDINGS.map(b => <option key={b} value={b}>{b}</option>)}
                  </select>
                </div>
              </div>
            </Section>
          </>
        )}

        {/* ════ TAB: DISTRIBUTION ════ */}
        {activeTab === 'distribution' && (
          <>
            <Section title="Паттерны распределения" icon={BarChart3} defaultOpen>
              <DistributionRules rules={c.distributionRules} onChange={r => update('distributionRules', r)} />
            </Section>

            <Section title="Связки активностей (зависимости)" icon={Layers} defaultOpen={false}>
              <div className="mb-3 p-3 bg-blue-50 border border-blue-100 rounded-xl flex items-start gap-2">
                <Info className="w-4 h-4 text-blue-500 shrink-0 mt-0.5" />
                <div className="text-[11px] text-blue-700 leading-relaxed">
                  <b>Parent → Child:</b> Задайте зависимости между занятиями. Например, «Лекция» должна предшествовать «Семинару» в тот же день.
                  Используйте «Одновременно» для объединения потоков.
                </div>
              </div>
              <RelationshipBuilder links={c.activityLinks} onChange={l => update('activityLinks', l)} />
            </Section>
          </>
        )}

        {/* ════ TAB: LOGISTICS ════ */}
        {activeTab === 'logistics' && (
          <Section title="Логистика и потоки" icon={Route} defaultOpen>
            <LogisticsModule
              travelRules={c.travelRules}
              streams={c.streams}
              onTravelChange={r => update('travelRules', r)}
              onStreamsChange={s => update('streams', s)}
            />
          </Section>
        )}

        {/* ════ TAB: CONTRACT ════ */}
        {activeTab === 'contract' && c.entityType === 'teacher' && (
          <Section title="Контрактные ограничения" icon={Briefcase} defaultOpen>
            <ContractModule contract={c.contract} onChange={cont => update('contract', cont)} />
          </Section>
        )}

        {/* ════ TAB: CONFLICTS ════ */}
        {activeTab === 'conflicts' && (
          <Section title="Анализ конфликтов и предложения" icon={AlertTriangle} defaultOpen>
            <ConflictEngine
              conflicts={visibleConflicts}
              onDismiss={handleDismiss}
              onRefresh={runConflictCheck}
              lastChecked={lastChecked}
            />
          </Section>
        )}

      </div>
    </div>
  );
}