import React, { useState, useMemo, useCallback, useEffect, useRef, createContext, useContext } from 'react';
import {
  User, MapPin, Users, Search, ChevronRight, ChevronDown,
  Building2, GraduationCap, SlidersHorizontal, BookOpen,
  Clock, Wrench, X, CheckCircle, Info, LayoutList,
  Filter, Layers, AlertCircle, Hash, Sun, Moon,
  School, Zap, Copy, Check, Database, Plus, Trash2, Save,
} from 'lucide-react';
import {
  institutes, departments, teachers, rooms as mockRooms, groups, buildings, subjects, workloadItems,
} from '../../store/mockData';
import type { WorkloadItem } from '../../store/mockData';

// ─── Equipment Context ────────────────────────────────────────────────────────

const BASE_EQUIPMENT = [
  'Проектор', 'Интерактивная доска', 'Компьютеры', 'Лабораторный стенд',
  'Осциллограф', '3D-принтер', 'Микроскоп', 'Графический планшет',
  'Звуковое оборудование', 'Видеокамера', 'Химический вытяжной шкаф', 'Паяльная станция',
  'Маркерная доска', 'Экран для проектора', 'Принтер', 'Сканер',
];

interface EquipCtxType { allEquipment: string[] }
const EquipCtx = createContext<EquipCtxType>({ allEquipment: BASE_EQUIPMENT });

// ─── Constants ────────────────────────────────────────────────────────────────

type CellState = 'required' | 'preferred' | 'neutral' | 'discouraged' | 'prohibited';
const DAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
const PERIODS = [1, 2, 3, 4, 5, 6, 7, 8];
const PERIOD_TIMES: Record<number, string> = {
  1: '08:00', 2: '09:40', 3: '11:30', 4: '13:10',
  5: '15:00', 6: '16:40', 7: '18:20', 8: '20:00',
};
const CELL_CYCLE: CellState[] = ['neutral', 'preferred', 'required', 'discouraged', 'prohibited'];
const CELL_STYLES: Record<CellState, { bg: string; border: string; label: string }> = {
  required:    { bg: 'bg-green-500',  border: 'border-green-600',  label: 'Обязат.'   },
  preferred:   { bg: 'bg-green-200',  border: 'border-green-400',  label: 'Предпочт.' },
  neutral:     { bg: 'bg-slate-100',  border: 'border-slate-200',  label: 'Нейтр.'    },
  discouraged: { bg: 'bg-yellow-200', border: 'border-yellow-400', label: 'Нежел.'    },
  prohibited:  { bg: 'bg-red-400',    border: 'border-red-500',    label: 'Запрещено' },
};

const EDU_LEVEL_LABELS: Record<string, string> = {
  bachelor: 'Бакалавриат', master: 'Магистратура', phd: 'Аспирантура', specialist: 'Специалитет',
};
const EDU_LEVEL_ORDER = ['bachelor', 'specialist', 'master', 'phd'];

const LESSON_TYPE_LABELS: Record<string, string> = { lecture: 'Лекция', practice: 'Практика', lab: 'Лаб. работа' };
const LESSON_TYPE_COLORS: Record<string, string> = {
  lecture: 'bg-blue-100 text-blue-700', practice: 'bg-teal-100 text-teal-700', lab: 'bg-purple-100 text-purple-700',
};

const ROOM_TYPES = [
  'Учебная аудитория', 'Компьютерный класс', 'Лекционный зал',
  'Лаборатория', 'Актовый зал', 'Спортивный зал', 'Конференц-зал',
];

// ─── Data Types ───────────────────────────────────────────────────────────────

export interface TimeConstraintData {
  timeGrid: Record<string, CellState>;
}
export interface RoomConstraintData {
  equipment: string[];
  timeGrid: Record<string, CellState>;
}
export interface GroupConstraintData { shift: 1 | 2 | null }
export interface WorkloadConstraintData {
  requiredEquipment: string[];
  isParallel: boolean;
  parallelSubgroups: string[];
  isDoublePair: boolean;
  preferredBuilding: string;
}

interface ManagedRoom {
  id: string;
  name: string;
  buildingId: string;
  capacity: number;
  type: string;
  equipment: string[];
}

type TabType = 'teacher' | 'room' | 'group' | 'workload' | 'manage';
type PanelKind = 'teacher_time' | 'room' | 'group_shift' | 'workload';

interface SelectedNode {
  key: string;
  displayName: string;
  sublabel: string;
  panelKind: PanelKind;
  levelLabel: string;
  isLeaf: boolean;
  workloadItem?: WorkloadItem;
  buildingId?: string;
  instituteId?: string;
}

// ─── Default Factories ────────────────────────────────────────────────────────

function makeDefaultTimeGrid(): Record<string, CellState> {
  const grid: Record<string, CellState> = {};
  DAYS.forEach((_, di) => { PERIODS.forEach(p => { grid[`${di}-${p}`] = 'neutral'; }); });
  return grid;
}
function makeDefaultTeacher(): TimeConstraintData {
  const grid = makeDefaultTimeGrid();
  [5, 6, 7, 8].forEach(p => { grid[`5-${p}`] = 'prohibited'; });
  return { timeGrid: grid };
}
const makeDefaultRoom = (): RoomConstraintData => ({ equipment: [], timeGrid: makeDefaultTimeGrid() });
const makeDefaultGroup = (): GroupConstraintData => ({ shift: null });
const makeDefaultWorkload = (item: WorkloadItem): WorkloadConstraintData => ({
  requiredEquipment: [], isParallel: false, parallelSubgroups: item.subgroups.slice(),
  isDoublePair: false, preferredBuilding: '',
});

// ─── TimeGrid ─────────────────────────────────────────────────────────────────

function TimeGrid({ grid, onChange }: { grid: Record<string, CellState>; onChange: (g: Record<string, CellState>) => void }) {
  const [isDragging, setIsDragging] = useState(false);
  const [dragState, setDragState] = useState<CellState>('neutral');
  const cycleState = useCallback((k: string): CellState => {
    const cur = grid[k] ?? 'neutral';
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
      <div className="flex flex-wrap gap-2 mb-2">
        {(Object.entries(CELL_STYLES) as [CellState, (typeof CELL_STYLES)[CellState]][]).map(([st, s]) => (
          <div key={st} className="flex items-center gap-1"><span className={`w-2.5 h-2.5 rounded-sm ${s.bg} border ${s.border}`} /><span className="text-[11px] text-slate-500">{s.label}</span></div>
        ))}
      </div>
      <div className="flex gap-1.5 mb-3 flex-wrap">
        <span className="text-[11px] text-slate-400 self-center">Заполнить:</span>
        {(['neutral', 'preferred', 'required', 'prohibited'] as CellState[]).map(s => (
          <button key={s} onClick={() => fillAll(s)} className={`text-[10px] px-2 py-0.5 rounded border ${CELL_STYLES[s].bg} ${CELL_STYLES[s].border} text-slate-700 hover:opacity-80`}>{CELL_STYLES[s].label}</button>
        ))}
      </div>
      <div className="overflow-x-auto">
        <div className="min-w-max" onMouseLeave={() => setIsDragging(false)}>
          <div className="flex mb-1"><div className="w-14 shrink-0" />{DAYS.map(d => <div key={d} className="w-8 text-center text-[11px] font-semibold text-slate-500">{d}</div>)}</div>
          {PERIODS.map(p => (
            <div key={p} className="flex items-center mb-0.5">
              <div className="w-14 shrink-0 pr-1 text-right"><span className="text-[10px] font-medium text-slate-500">{p}п</span><span className="text-[9px] text-slate-400 block">{PERIOD_TIMES[p]}</span></div>
              {DAYS.map((_, di) => {
                const key = `${di}-${p}`; const state = grid[key] ?? 'neutral'; const { bg, border } = CELL_STYLES[state];
                return <div key={key} onMouseDown={() => { const next = cycleState(key); setDragState(next); setIsDragging(true); onChange({ ...grid, [key]: next }); }} onMouseEnter={() => { if (isDragging) onChange({ ...grid, [key]: dragState }); }} title={CELL_STYLES[state].label} className={`w-8 h-7 mx-0.5 rounded cursor-pointer border-2 transition-all hover:scale-105 hover:shadow-md ${bg} ${border}`} />;
              })}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ─── EquipmentInput ───────────────────────────────────────────────────────────

function EquipmentInput({ tags, onChange, placeholder }: { tags: string[]; onChange: (t: string[]) => void; placeholder?: string }) {
  const { allEquipment } = useContext(EquipCtx);
  const [input, setInput] = useState('');
  const [show, setShow] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const filtered = allEquipment.filter(s => !tags.includes(s) && s.toLowerCase().includes(input.toLowerCase()));
  useEffect(() => {
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setShow(false); };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, []);
  const add = (t: string) => { const v = t.trim(); if (v && !tags.includes(v)) onChange([...tags, v]); setInput(''); setShow(false); };
  return (
    <div ref={ref} className="relative">
      <div className="flex flex-wrap gap-1.5 p-2 min-h-[2.5rem] border border-slate-200 rounded-lg bg-white focus-within:ring-2 focus-within:ring-[rgb(26,77,156)]/30">
        {tags.map(tag => <span key={tag} className="inline-flex items-center gap-1 bg-[rgb(26,77,156)]/10 text-[rgb(26,77,156)] text-xs px-2 py-0.5 rounded-full border border-[rgb(26,77,156)]/20">{tag}<button onClick={() => onChange(tags.filter(t => t !== tag))} className="hover:text-red-500"><X className="w-3 h-3" /></button></span>)}
        <input value={input} onChange={e => { setInput(e.target.value); setShow(true); }} onFocus={() => setShow(true)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add(input); } if (e.key === 'Backspace' && !input && tags.length) onChange(tags.slice(0, -1)); }}
          placeholder={tags.length === 0 ? (placeholder ?? 'Добавить…') : ''} className="flex-1 min-w-[8rem] outline-none text-sm bg-transparent placeholder-slate-400" />
      </div>
      {show && (input || filtered.length > 0) && (
        <div className="absolute z-50 left-0 right-0 mt-1 bg-white border border-slate-200 rounded-lg shadow-xl overflow-hidden">
          {input && !allEquipment.some(s => s.toLowerCase() === input.toLowerCase()) && (
            <button onMouseDown={e => { e.preventDefault(); add(input); }} className="w-full text-left px-3 py-2 text-sm text-[rgb(26,77,156)] hover:bg-[rgb(26,77,156)]/5 flex items-center gap-2">
              <Plus className="w-3.5 h-3.5" />Создать «{input}»
            </button>
          )}
          {filtered.slice(0, 6).map(s => <button key={s} onMouseDown={e => { e.preventDefault(); add(s); }} className="w-full text-left px-3 py-2 text-sm text-slate-700 hover:bg-[rgb(26,77,156)]/5 flex items-center gap-2"><Wrench className="w-3.5 h-3.5 text-slate-400" />{s}</button>)}
        </div>
      )}
    </div>
  );
}

// ─── Section ──────────────────────────────────────────────────────────────────

function Section({ title, icon: Icon, defaultOpen = true, children }: { title: string; icon: React.FC<{ className?: string }>; defaultOpen?: boolean; children: React.ReactNode }) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border border-slate-200 rounded-xl overflow-hidden shadow-sm">
      <button onClick={() => setOpen(o => !o)} className={`w-full flex items-center gap-3 px-4 py-3 text-left transition-colors ${open ? 'bg-slate-50 border-b border-slate-200' : 'bg-white hover:bg-slate-50'}`}>
        <span className="p-1.5 rounded-lg bg-[rgb(26,77,156)]/10"><Icon className="w-4 h-4 text-[rgb(26,77,156)]" /></span>
        <span className="flex-1 text-sm font-semibold text-slate-700">{title}</span>
        {open ? <ChevronDown className="w-4 h-4 text-slate-400" /> : <ChevronRight className="w-4 h-4 text-slate-400" />}
      </button>
      {open && <div className="bg-white p-4">{children}</div>}
    </div>
  );
}

// ─── Panel: Teacher ───────────────────────────────────────────────────────────

function TeacherPanel({ node, constraint, onChange, onSave }: { node: SelectedNode; constraint: TimeConstraintData; onChange: (c: TimeConstraintData) => void; onSave: () => void }) {
  const [flash, setFlash] = useState(false);
  const save = () => { onSave(); setFlash(true); setTimeout(() => setFlash(false), 2000); };
  return (
    <div className="flex flex-col h-full bg-slate-50 overflow-hidden">
      <div className="bg-white border-b border-slate-200 px-5 py-3.5 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-[rgb(26,77,156)] flex items-center justify-center shrink-0">{node.isLeaf ? <User className="w-5 h-5 text-white" /> : <GraduationCap className="w-5 h-5 text-white" />}</div>
          <div className="flex-1 min-w-0"><h2 className="text-sm font-bold text-slate-800 truncate">{node.displayName}</h2><p className="text-[11px] text-slate-400">{node.levelLabel}{node.sublabel ? ` · ${node.sublabel}` : ''}</p></div>
          <button onClick={save} className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg transition-all ${flash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{flash ? <><CheckCircle className="w-3.5 h-3.5" />Сохранено</> : 'Сохранить'}</button>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {!node.isLeaf && <div className="flex items-start gap-2 p-3 bg-blue-50 border border-blue-100 rounded-xl"><Info className="w-4 h-4 text-blue-500 shrink-0 mt-0.5" /><p className="text-[12px] text-blue-700 leading-relaxed">Настройки применяются ко всем преподавателям {node.levelLabel === 'Кафедра' ? 'этой кафедры' : 'этого института'}. Индивидуальные настройки имеют приоритет.</p></div>}
        <Section title="Доступность по времени" icon={Clock}>
          <TimeGrid grid={constraint.timeGrid} onChange={g => onChange({ ...constraint, timeGrid: g })} />
        </Section>
      </div>
    </div>
  );
}

// ─── Panel: Room ──────────────────────────────────────────────────────────────

function RoomPanel({ node, constraint, onChange, onSave }: { node: SelectedNode; constraint: RoomConstraintData; onChange: (c: RoomConstraintData) => void; onSave: () => void }) {
  const [flash, setFlash] = useState(false);
  const save = () => { onSave(); setFlash(true); setTimeout(() => setFlash(false), 2000); };
  return (
    <div className="flex flex-col h-full bg-slate-50 overflow-hidden">
      <div className="bg-white border-b border-slate-200 px-5 py-3.5 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-purple-600 flex items-center justify-center shrink-0">{node.isLeaf ? <MapPin className="w-5 h-5 text-white" /> : <Building2 className="w-5 h-5 text-white" />}</div>
          <div className="flex-1 min-w-0"><h2 className="text-sm font-bold text-slate-800 truncate">{node.displayName}</h2><p className="text-[11px] text-slate-400">{node.levelLabel}{node.sublabel ? ` · ${node.sublabel}` : ''}</p></div>
          <button onClick={save} className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg transition-all ${flash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{flash ? <><CheckCircle className="w-3.5 h-3.5" />Сохранено</> : 'Сохранить'}</button>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {!node.isLeaf && <div className="flex items-start gap-2 p-3 bg-purple-50 border border-purple-100 rounded-xl"><Info className="w-4 h-4 text-purple-500 shrink-0 mt-0.5" /><p className="text-[12px] text-purple-700 leading-relaxed">Настройки применяются ко всем аудиториям корпуса.</p></div>}
        <Section title={node.isLeaf ? 'Доступное оборудование' : 'Оборудование по умолчанию'} icon={Wrench}>
          <EquipmentInput tags={constraint.equipment} onChange={eq => onChange({ ...constraint, equipment: eq })} placeholder="Добавить оборудование…" />
        </Section>
        <Section title="Доступность по времени" icon={Clock}>
          <TimeGrid grid={constraint.timeGrid} onChange={g => onChange({ ...constraint, timeGrid: g })} />
        </Section>
      </div>
    </div>
  );
}

// ─── Panel: Group ─────────────────────────────────────────────────────────────

function GroupPanel({ node, constraint, onChange, onSave }: { node: SelectedNode; constraint: GroupConstraintData; onChange: (c: GroupConstraintData) => void; onSave: () => void }) {
  const [flash, setFlash] = useState(false);
  const save = () => { onSave(); setFlash(true); setTimeout(() => setFlash(false), 2000); };
  const shifts: { val: 1 | 2 | null; label: string; desc: string; icon: React.FC<{ className?: string }>; color: string }[] = [
    { val: null, label: 'Не указана', desc: 'Без ограничений', icon: AlertCircle, color: 'border-slate-200 text-slate-500 bg-white' },
    { val: 1,    label: 'Смена 1',   desc: '08:00 — 14:00',  icon: Sun,         color: 'border-amber-300 text-amber-700 bg-amber-50' },
    { val: 2,    label: 'Смена 2',   desc: '14:00 — 20:00',  icon: Moon,        color: 'border-indigo-300 text-indigo-700 bg-indigo-50' },
  ];
  return (
    <div className="flex flex-col h-full bg-slate-50 overflow-hidden">
      <div className="bg-white border-b border-slate-200 px-5 py-3.5 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-teal-600 flex items-center justify-center shrink-0"><Users className="w-5 h-5 text-white" /></div>
          <div className="flex-1 min-w-0"><h2 className="text-sm font-bold text-slate-800 truncate">{node.displayName}</h2><p className="text-[11px] text-slate-400">{node.levelLabel}{node.sublabel ? ` · ${node.sublabel}` : ''}</p></div>
          <button onClick={save} className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg transition-all ${flash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{flash ? <><CheckCircle className="w-3.5 h-3.5" />Сохранено</> : 'Сохранить'}</button>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {!node.isLeaf && <div className="flex items-start gap-2 p-3 bg-teal-50 border border-teal-100 rounded-xl"><Info className="w-4 h-4 text-teal-500 shrink-0 mt-0.5" /><p className="text-[12px] text-teal-700 leading-relaxed">Настройки смены применяются ко всем группам {node.levelLabel === 'Институт' ? 'этого института' : node.levelLabel === 'Курс' ? 'этого курса' : 'этого уровня образования'}.</p></div>}
        <Section title="Смена" icon={Clock}>
          <div className="grid grid-cols-3 gap-3">
            {shifts.map(s => {
              const SIcon = s.icon; const isActive = constraint.shift === s.val;
              return <button key={String(s.val)} onClick={() => onChange({ shift: s.val })} className={`flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-all ${isActive ? `${s.color} ring-2 ring-offset-1 ring-[rgb(26,77,156)]` : `${s.color} opacity-60 hover:opacity-100`}`}><SIcon className="w-6 h-6" /><span className="text-sm font-semibold">{s.label}</span><span className="text-[11px] opacity-75">{s.desc}</span>{isActive && <Check className="w-4 h-4 text-[rgb(26,77,156)]" />}</button>;
            })}
          </div>
        </Section>
      </div>
    </div>
  );
}

// ─── Panel: Workload ──────────────────────────────────────────────────────────

const MASS_SCOPES = [
  { val: 'item',      label: 'Только эта запись',          color: 'bg-slate-100 text-slate-700 border-slate-200' },
  { val: 'subject',   label: 'Весь предмет',               color: 'bg-orange-100 text-orange-700 border-orange-200' },
  { val: 'teacher',   label: 'Все нагрузки преподавателя', color: 'bg-blue-100 text-blue-700 border-blue-200' },
  { val: 'group',     label: 'Все нагрузки группы',        color: 'bg-teal-100 text-teal-700 border-teal-200' },
  { val: 'institute', label: 'Весь институт',              color: 'bg-indigo-100 text-indigo-700 border-indigo-200' },
  { val: 'all',       label: 'Вся нагрузка',               color: 'bg-red-100 text-red-700 border-red-200' },
] as const;
type MassScope = (typeof MASS_SCOPES)[number]['val'];

function WorkloadPanel({ node, constraint, onChange, onSave, onMassApply }: { node: SelectedNode; constraint: WorkloadConstraintData; onChange: (c: WorkloadConstraintData) => void; onSave: () => void; onMassApply: (scope: MassScope) => void }) {
  const [flash, setFlash] = useState(false);
  const [massScope, setMassScope] = useState<MassScope>('item');
  const [massFlash, setMassFlash] = useState(false);
  const item = node.workloadItem!;
  const teacher = teachers.find(t => t.id === item.teacherId);
  const group = groups.find(g => g.id === item.groupId);
  const subject = subjects.find(s => s.id === item.subjectId);
  const institute = group ? institutes.find(i => i.id === group.instituteId) : undefined;
  const save = () => { onSave(); setFlash(true); setTimeout(() => setFlash(false), 2000); };
  const doMass = () => { onMassApply(massScope); setMassFlash(true); setTimeout(() => setMassFlash(false), 2000); };
  const scopeLabels: Record<MassScope, string> = {
    item: 'Только эта запись нагрузки',
    subject: `Весь предмет: ${subject?.name ?? '—'}`,
    teacher: `Все нагрузки: ${teacher?.name ?? '—'}`,
    group: `Все нагрузки группы: ${group?.name ?? '—'}`,
    institute: `Весь институт: ${institute?.name ?? '—'}`,
    all: 'Вся нагрузка (все записи)',
  };
  return (
    <div className="flex flex-col h-full bg-slate-50 overflow-hidden">
      <div className="bg-white border-b border-slate-200 px-5 py-3.5 shrink-0">
        <div className="flex items-start gap-3">
          <div className="w-9 h-9 rounded-xl bg-orange-500 flex items-center justify-center shrink-0 mt-0.5"><BookOpen className="w-5 h-5 text-white" /></div>
          <div className="flex-1 min-w-0 space-y-1">
            <h2 className="text-sm font-bold text-slate-800 truncate">{subject?.name ?? '—'}</h2>
            <div className="flex flex-wrap gap-1.5">
              <span className="inline-flex items-center gap-1 bg-blue-50 text-blue-700 text-[11px] px-2 py-0.5 rounded-full border border-blue-100"><User className="w-3 h-3" />{teacher?.name ?? '—'}</span>
              <span className="inline-flex items-center gap-1 bg-teal-50 text-teal-700 text-[11px] px-2 py-0.5 rounded-full border border-teal-100"><Users className="w-3 h-3" />{group?.name ?? '—'}</span>
              <span className={`text-[11px] px-2 py-0.5 rounded-full ${LESSON_TYPE_COLORS[item.lessonType]}`}>{LESSON_TYPE_LABELS[item.lessonType]}</span>
              <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">{item.hoursPerWeek} ч/нед</span>
              {item.subgroups.length > 0 && <span className="text-[11px] px-2 py-0.5 rounded-full bg-purple-50 text-purple-700 border border-purple-100">{item.subgroups.length} подгр.</span>}
            </div>
          </div>
          <button onClick={save} className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg transition-all shrink-0 ${flash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{flash ? <><CheckCircle className="w-3.5 h-3.5" />Сохранено</> : 'Сохранить'}</button>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        <Section title="Требование к оборудованию" icon={Wrench}>
          <EquipmentInput tags={constraint.requiredEquipment} onChange={eq => onChange({ ...constraint, requiredEquipment: eq })} placeholder="Добавить требуемое оборудование…" />
          <p className="text-[11px] text-slate-400 mt-2">При подборе аудитории будут учитываться только те, которые имеют указанное оборудование.</p>
        </Section>
        <Section title="Параллельность" icon={Layers}>
          <div className="flex items-center justify-between mb-3">
            <div><p className="text-sm font-medium text-slate-700">Параллельные занятия</p><p className="text-[11px] text-slate-400 mt-0.5">Подгруппы занимаются одновременно в разных аудиториях</p></div>
            <button onClick={() => onChange({ ...constraint, isParallel: !constraint.isParallel })} className={`relative w-11 h-6 rounded-full transition-colors ${constraint.isParallel ? 'bg-[rgb(26,77,156)]' : 'bg-slate-200'}`}><span className={`absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform ${constraint.isParallel ? 'translate-x-5' : 'translate-x-0.5'}`} /></button>
          </div>
          {constraint.isParallel && (
            <div className="mt-2 space-y-2">
              {item.subgroups.length > 0 ? (
                <>{item.subgroups.map(sg => { const checked = constraint.parallelSubgroups.includes(sg); return <label key={sg} className="flex items-center gap-2.5 p-2.5 rounded-lg border border-slate-200 bg-slate-50 cursor-pointer hover:bg-slate-100"><input type="checkbox" checked={checked} onChange={() => { const next = checked ? constraint.parallelSubgroups.filter(s => s !== sg) : [...constraint.parallelSubgroups, sg]; onChange({ ...constraint, parallelSubgroups: next }); }} className="w-4 h-4 accent-[rgb(26,77,156)]" /><span className="text-sm text-slate-700">{sg}</span></label>; })}<div className="p-2.5 bg-blue-50 rounded-lg border border-blue-100"><p className="text-[11px] text-blue-700">Отмеченные подгруппы будут ставиться в расписание одновременно.</p></div></>
              ) : <div className="p-3 bg-amber-50 border border-amber-100 rounded-lg"><p className="text-[12px] text-amber-700">У данной записи нагрузки нет подгрупп.</p></div>}
            </div>
          )}
        </Section>
        <Section title="Двойная пара" icon={Copy}>
          <div className="flex items-center justify-between">
            <div><p className="text-sm font-medium text-slate-700">Ставить двойной парой</p><p className="text-[11px] text-slate-400 mt-0.5">Две пары подряд без перерыва</p></div>
            <button onClick={() => onChange({ ...constraint, isDoublePair: !constraint.isDoublePair })} className={`relative w-11 h-6 rounded-full transition-colors ${constraint.isDoublePair ? 'bg-[rgb(26,77,156)]' : 'bg-slate-200'}`}><span className={`absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform ${constraint.isDoublePair ? 'translate-x-5' : 'translate-x-0.5'}`} /></button>
          </div>
          {constraint.isDoublePair && <div className="mt-3 p-2.5 bg-blue-50 rounded-lg border border-blue-100"><p className="text-[11px] text-blue-700">Занятия всегда будут планироваться парами: 2 пары подряд в один день.</p></div>}
        </Section>
        <Section title="Предпочтительный корпус" icon={Building2}>
          <select value={constraint.preferredBuilding} onChange={e => onChange({ ...constraint, preferredBuilding: e.target.value })} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
            <option value="">Без предпочтений</option>
            {buildings.map(b => <option key={b.id} value={b.id}>{b.name} ({b.address})</option>)}
          </select>
        </Section>
        {/* Mass apply */}
        <div className="border-2 border-dashed border-[rgb(26,77,156)]/30 rounded-xl p-4 bg-[rgb(26,77,156)]/3">
          <div className="flex items-center gap-2 mb-3"><Zap className="w-4 h-4 text-[rgb(26,77,156)]" /><span className="text-sm font-bold text-[rgb(26,77,156)]">Массовое применение</span></div>
          <div className="space-y-1.5 mb-3">
            {MASS_SCOPES.map(s => <label key={s.val} className={`flex items-center gap-2.5 p-2 rounded-lg border cursor-pointer transition-all ${massScope === s.val ? `${s.color} border-current ring-1 ring-current` : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'}`}><input type="radio" name="massScope" value={s.val} checked={massScope === s.val} onChange={() => setMassScope(s.val)} className="w-3.5 h-3.5" /><span className="text-[12px] font-medium">{scopeLabels[s.val]}</span></label>)}
          </div>
          {massScope !== 'item' && <div className="p-2.5 bg-amber-50 border border-amber-200 rounded-lg mb-3"><p className="text-[11px] text-amber-700">⚠ Перезапишет ограничения всех затронутых записей.</p></div>}
          <button onClick={doMass} className={`w-full flex items-center justify-center gap-2 py-2.5 rounded-lg text-sm font-semibold transition-all ${massFlash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{massFlash ? <><CheckCircle className="w-4 h-4" />Применено!</> : <><Zap className="w-4 h-4" />Применить массово</>}</button>
        </div>
      </div>
    </div>
  );
}

// ─── Panel: Manage (Rooms + Equipment) ───────────────────────────────────────

interface ManagePanelProps {
  managedRooms: ManagedRoom[];
  onRoomsChange: (r: ManagedRoom[]) => void;
  customEquipment: string[];
  onEquipmentChange: (e: string[]) => void;
}

function ManagePanel({ managedRooms, onRoomsChange, customEquipment, onEquipmentChange }: ManagePanelProps) {
  const [subTab, setSubTab] = useState<'rooms' | 'equipment'>('rooms');
  const [selectedRoomId, setSelectedRoomId] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [roomForm, setRoomForm] = useState<Omit<ManagedRoom, 'id'>>({ name: '', buildingId: buildings[0]?.id ?? '', capacity: 30, type: ROOM_TYPES[0], equipment: [] });
  const [newEquipName, setNewEquipName] = useState('');
  const [flash, setFlash] = useState(false);
  const { allEquipment } = useContext(EquipCtx);

  const selectedRoom = managedRooms.find(r => r.id === selectedRoomId);

  const startCreate = () => {
    setIsCreating(true);
    setSelectedRoomId(null);
    setRoomForm({ name: '', buildingId: buildings[0]?.id ?? '', capacity: 30, type: ROOM_TYPES[0], equipment: [] });
  };

  const startEdit = (room: ManagedRoom) => {
    setIsCreating(false);
    setSelectedRoomId(room.id);
    setRoomForm({ name: room.name, buildingId: room.buildingId, capacity: room.capacity, type: room.type, equipment: room.equipment });
  };

  const saveRoom = () => {
    if (!roomForm.name.trim()) return;
    if (isCreating) {
      const newRoom: ManagedRoom = { ...roomForm, id: `room_${Date.now()}` };
      onRoomsChange([...managedRooms, newRoom]);
      setSelectedRoomId(newRoom.id);
    } else if (selectedRoomId) {
      onRoomsChange(managedRooms.map(r => r.id === selectedRoomId ? { ...r, ...roomForm } : r));
    }
    setIsCreating(false);
    setFlash(true); setTimeout(() => setFlash(false), 2000);
  };

  const deleteRoom = (id: string) => {
    if (!confirm('Удалить аудиторию?')) return;
    onRoomsChange(managedRooms.filter(r => r.id !== id));
    if (selectedRoomId === id) { setSelectedRoomId(null); setIsCreating(false); }
  };

  const addEquip = () => {
    const v = newEquipName.trim();
    if (v && !allEquipment.includes(v)) { onEquipmentChange([...customEquipment, v]); }
    setNewEquipName('');
  };

  const deleteEquip = (name: string) => { onEquipmentChange(customEquipment.filter(e => e !== name)); };

  return (
    <div className="flex h-full overflow-hidden">
      {/* Left: list */}
      <div className="w-72 shrink-0 border-r border-slate-200 flex flex-col bg-white">
        {/* Sub-tabs */}
        <div className="flex border-b border-slate-100 shrink-0">
          {[{ val: 'rooms' as const, label: 'Аудитории', icon: MapPin }, { val: 'equipment' as const, label: 'Оборудование', icon: Wrench }].map(t => {
            const TIcon = t.icon;
            return <button key={t.val} onClick={() => setSubTab(t.val)} className={`flex-1 flex items-center justify-center gap-1.5 py-3 text-xs font-medium border-b-2 transition-all ${subTab === t.val ? 'border-[rgb(26,77,156)] text-[rgb(26,77,156)] bg-white' : 'border-transparent text-slate-400 hover:text-slate-600 hover:bg-slate-50'}`}><TIcon className="w-3.5 h-3.5" />{t.label}</button>;
          })}
        </div>

        {subTab === 'rooms' && (
          <>
            <div className="p-3 border-b border-slate-100 shrink-0">
              <button onClick={startCreate} className="w-full flex items-center justify-center gap-2 py-2 rounded-lg bg-[rgb(26,77,156)] text-white text-xs font-semibold hover:bg-[rgb(20,60,130)] transition-colors shadow-sm">
                <Plus className="w-3.5 h-3.5" />Создать аудиторию
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-2 space-y-1">
              {managedRooms.map(room => {
                const bld = buildings.find(b => b.id === room.buildingId);
                const isSelected = selectedRoomId === room.id && !isCreating;
                return (
                  <div key={room.id} className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-all ${isSelected ? 'bg-[rgb(26,77,156)] text-white' : 'hover:bg-slate-100'}`}>
                    <button onClick={() => startEdit(room)} className="flex-1 text-left min-w-0">
                      <div className={`text-xs font-semibold truncate ${isSelected ? 'text-white' : 'text-slate-800'}`}>Ауд. {room.name}</div>
                      <div className={`text-[10px] truncate ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}>{bld?.name} · {room.capacity} мест</div>
                    </button>
                    <button onClick={() => deleteRoom(room.id)} className={`p-1 rounded hover:bg-red-100 hover:text-red-600 transition-colors ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}><Trash2 className="w-3 h-3" /></button>
                  </div>
                );
              })}
              {managedRooms.length === 0 && <div className="text-center py-8 text-slate-400 text-sm"><MapPin className="w-6 h-6 mx-auto mb-2 text-slate-300" />Нет аудиторий</div>}
            </div>
          </>
        )}

        {subTab === 'equipment' && (
          <>
            <div className="p-3 border-b border-slate-100 shrink-0 space-y-2">
              <div className="flex gap-2">
                <input value={newEquipName} onChange={e => setNewEquipName(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') addEquip(); }}
                  placeholder="Название оборудования…"
                  className="flex-1 text-xs border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30" />
                <button onClick={addEquip} className="px-3 py-2 rounded-lg bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] transition-colors"><Plus className="w-3.5 h-3.5" /></button>
              </div>
            </div>
            <div className="flex-1 overflow-y-auto p-2 space-y-0.5">
              <p className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider px-2 py-1">Базовое</p>
              {BASE_EQUIPMENT.map(e => <div key={e} className="flex items-center gap-2 px-3 py-1.5 rounded-lg"><Wrench className="w-3 h-3 text-slate-400 shrink-0" /><span className="text-xs text-slate-600 flex-1">{e}</span></div>)}
              {customEquipment.length > 0 && <>
                <p className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider px-2 py-1 mt-2">Созданное</p>
                {customEquipment.map(e => <div key={e} className="flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-slate-100 group"><Wrench className="w-3 h-3 text-[rgb(26,77,156)] shrink-0" /><span className="text-xs text-slate-700 flex-1">{e}</span><button onClick={() => deleteEquip(e)} className="opacity-0 group-hover:opacity-100 p-0.5 rounded hover:text-red-600 transition-all"><Trash2 className="w-3 h-3" /></button></div>)}
              </>}
            </div>
          </>
        )}
      </div>

      {/* Right: form */}
      <div className="flex-1 overflow-y-auto">
        {(isCreating || (subTab === 'rooms' && selectedRoom)) && (
          <div className="p-5 space-y-4">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-9 h-9 rounded-xl bg-purple-600 flex items-center justify-center"><MapPin className="w-5 h-5 text-white" /></div>
              <div><h3 className="text-sm font-bold text-slate-800">{isCreating ? 'Новая аудитория' : `Ред. ауд. ${selectedRoom?.name}`}</h3><p className="text-[11px] text-slate-400">Заполните параметры аудитории</p></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Номер / Название</label>
                <input value={roomForm.name} onChange={e => setRoomForm(f => ({ ...f, name: e.target.value }))} placeholder="например, 305" className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30" />
              </div>
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Корпус</label>
                <select value={roomForm.buildingId} onChange={e => setRoomForm(f => ({ ...f, buildingId: e.target.value }))} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
                  {buildings.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Вместимость (мест)</label>
                <input type="number" min={1} max={1000} value={roomForm.capacity} onChange={e => setRoomForm(f => ({ ...f, capacity: Number(e.target.value) }))} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30" />
              </div>
              <div>
                <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Тип аудитории</label>
                <select value={roomForm.type} onChange={e => setRoomForm(f => ({ ...f, type: e.target.value }))} className="w-full text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30">
                  {ROOM_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
            </div>
            <div>
              <label className="block text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Оборудование</label>
              <EquipmentInput tags={roomForm.equipment} onChange={eq => setRoomForm(f => ({ ...f, equipment: eq }))} placeholder="Добавить оборудование…" />
            </div>
            <button onClick={saveRoom} className={`flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold transition-all ${flash ? 'bg-green-100 text-green-700 border border-green-200' : 'bg-[rgb(26,77,156)] text-white hover:bg-[rgb(20,60,130)] shadow-sm'}`}>{flash ? <><CheckCircle className="w-4 h-4" />Сохранено</> : <><Save className="w-4 h-4" />Сохранить</>}</button>
          </div>
        )}
        {!isCreating && !selectedRoom && subTab === 'rooms' && (
          <div className="flex flex-col items-center justify-center h-full text-center p-10">
            <div className="w-16 h-16 rounded-2xl bg-purple-100 flex items-center justify-center mb-4"><MapPin className="w-8 h-8 text-purple-400" /></div>
            <h3 className="text-base font-semibold text-slate-600 mb-2">Управление аудиториями</h3>
            <p className="text-sm text-slate-400 max-w-xs leading-relaxed">Выберите аудиторию для редактирования или создайте новую.</p>
          </div>
        )}
        {subTab === 'equipment' && (
          <div className="flex flex-col items-center justify-center h-full text-center p-10">
            <div className="w-16 h-16 rounded-2xl bg-[rgb(26,77,156)]/10 flex items-center justify-center mb-4"><Wrench className="w-8 h-8 text-[rgb(26,77,156)]/50" /></div>
            <h3 className="text-base font-semibold text-slate-600 mb-2">Справочник оборудования</h3>
            <p className="text-sm text-slate-400 max-w-xs leading-relaxed">Добавляйте новые типы оборудования. Они будут доступны при настройке аудиторий и ограничений нагрузки.</p>
            <div className="mt-4 text-[11px] text-slate-400">{BASE_EQUIPMENT.length + customEquipment.length} типов оборудования в системе</div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Empty State ──────────────────────────────────────────────────────────────

function EmptyState({ onTabChange }: { onTabChange: (t: TabType) => void }) {
  const tabs: { type: TabType; label: string; icon: React.FC<{ className?: string }>; color: string; count: number }[] = [
    { type: 'teacher',  label: 'Преподаватели', icon: User,    color: 'bg-blue-50 text-blue-700 border-blue-200',     count: teachers.length },
    { type: 'room',     label: 'Аудитории',     icon: MapPin,  color: 'bg-purple-50 text-purple-700 border-purple-200', count: mockRooms.length },
    { type: 'group',    label: 'Группы',        icon: Users,   color: 'bg-teal-50 text-teal-700 border-teal-200',     count: groups.length },
    { type: 'workload', label: 'Нагрузка',      icon: BookOpen,color: 'bg-orange-50 text-orange-700 border-orange-200',count: workloadItems.length },
  ];
  return (
    <div className="flex flex-col items-center justify-center h-full text-center p-10">
      <div className="w-16 h-16 rounded-2xl bg-[rgb(26,77,156)]/10 flex items-center justify-center mb-4"><SlidersHorizontal className="w-8 h-8 text-[rgb(26,77,156)]/50" /></div>
      <h3 className="text-base font-semibold text-slate-600 mb-2">Выберите объект</h3>
      <p className="text-sm text-slate-400 max-w-xs leading-relaxed mb-6">Выберите преподавателя, аудиторию, группу или запись нагрузки для настройки ограничений.</p>
      <div className="grid grid-cols-2 gap-3 w-full max-w-sm">
        {tabs.map(t => { const TIcon = t.icon; return <button key={t.type} onClick={() => onTabChange(t.type)} className={`flex items-center gap-2.5 p-3 rounded-xl border text-left transition-all hover:shadow-sm ${t.color}`}><TIcon className="w-5 h-5 shrink-0" /><div><div className="text-xs font-semibold">{t.label}</div><div className="text-[10px] opacity-70">{t.count} объектов</div></div></button>; })}
      </div>
    </div>
  );
}

// ─── Main: PropertiesManager ──────────────────────────────────────────────────

export function PropertiesManager() {
  const [activeTab, setActiveTab] = useState<TabType>('teacher');
  const [search, setSearch] = useState('');
  const [selectedNode, setSelectedNode] = useState<SelectedNode | null>(null);

  // Expand state
  const [expandInstitutes, setExpandInstitutes] = useState<Record<string, boolean>>({});
  const [expandDepts, setExpandDepts] = useState<Record<string, boolean>>({});
  const [expandBuildings, setExpandBuildings] = useState<Record<string, boolean>>({});
  const [expandGroupInst, setExpandGroupInst] = useState<Record<string, boolean>>({});
  const [expandEduLevel, setExpandEduLevel] = useState<Record<string, boolean>>({});
  const [expandCourse, setExpandCourse] = useState<Record<string, boolean>>({});

  // Workload filters
  const [wlTeacher, setWlTeacher] = useState('');
  const [wlGroup, setWlGroup] = useState('');
  const [wlSubject, setWlSubject] = useState('');

  // Constraint storage
  const [timeConstraints, setTimeConstraints] = useState<Record<string, TimeConstraintData>>({});
  const [roomConstraints, setRoomConstraints] = useState<Record<string, RoomConstraintData>>({});
  const [groupConstraints, setGroupConstraints] = useState<Record<string, GroupConstraintData>>({});
  const [workloadConstraints, setWorkloadConstraints] = useState<Record<string, WorkloadConstraintData>>({});

  // Manage state
  const [customEquipment, setCustomEquipment] = useState<string[]>([]);
  const [managedRooms, setManagedRooms] = useState<ManagedRoom[]>(() =>
    mockRooms.map(r => ({ id: r.id, name: r.name, buildingId: r.buildingId, capacity: r.capacity, type: r.type === 'computer_lab' ? 'Компьютерный класс' : r.type === 'lecture_hall' ? 'Лекционный зал' : r.type === 'lab' ? 'Лаборатория' : r.type === 'hall' ? 'Актовый зал' : 'Учебная аудитория', equipment: r.properties ?? [] }))
  );

  const allEquipment = useMemo(() => [...BASE_EQUIPMENT, ...customEquipment.filter(e => !BASE_EQUIPMENT.includes(e))], [customEquipment]);

  // Helpers
  const isOpen = (map: Record<string, boolean>, id: string) => map[id] !== false;
  const toggle = (set: React.Dispatch<React.SetStateAction<Record<string, boolean>>>, id: string) => set(prev => ({ ...prev, [id]: !isOpen(prev, id) }));
  const hasSaved = (key: string, kind: PanelKind) => {
    if (kind === 'teacher_time') return !!timeConstraints[key];
    if (kind === 'room') return !!roomConstraints[key];
    if (kind === 'group_shift') return !!groupConstraints[key];
    if (kind === 'workload') return !!workloadConstraints[key];
    return false;
  };

  const filteredWorkload = useMemo(() => {
    let items = workloadItems;
    if (wlTeacher) items = items.filter(w => w.teacherId === wlTeacher);
    if (wlGroup) items = items.filter(w => w.groupId === wlGroup);
    if (wlSubject) items = items.filter(w => w.subjectId === wlSubject);
    if (search && activeTab === 'workload') {
      const q = search.toLowerCase();
      items = items.filter(w => {
        const t = teachers.find(x => x.id === w.teacherId);
        const g = groups.find(x => x.id === w.groupId);
        const s = subjects.find(x => x.id === w.subjectId);
        return t?.name.toLowerCase().includes(q) || g?.name.toLowerCase().includes(q) || s?.name.toLowerCase().includes(q);
      });
    }
    return items;
  }, [wlTeacher, wlGroup, wlSubject, search, activeTab]);

  const renderEntityBtn = (node: SelectedNode) => {
    const isSelected = selectedNode?.key === node.key;
    const saved = hasSaved(node.key, node.panelKind);
    return (
      <button key={node.key} onClick={() => setSelectedNode(node)}
        className={`w-full flex items-center gap-2 px-3 py-2 rounded-lg text-left transition-all ${isSelected ? 'bg-[rgb(26,77,156)] text-white shadow-sm' : 'hover:bg-slate-100 text-slate-700'}`}>
        <div className="flex-1 min-w-0">
          <div className="text-xs font-medium truncate">{node.displayName}</div>
          {node.sublabel && <div className={`text-[10px] truncate ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}>{node.sublabel}</div>}
        </div>
        {saved && <span className={`w-2 h-2 rounded-full shrink-0 ${isSelected ? 'bg-green-300' : 'bg-green-500'}`} />}
      </button>
    );
  };

  const renderBranchBtn = (key: string, panelKind: PanelKind, levelLabel: string, name: string, sublabel: string, open: boolean, Icon: React.FC<{ className?: string }>, iconColor: string, onToggle: () => void, count: number) => {
    const isSelected = selectedNode?.key === key;
    const saved = hasSaved(key, panelKind);
    return (
      <div key={key} className="flex items-center gap-0.5">
        <button onClick={onToggle} className="w-6 h-7 flex items-center justify-center hover:bg-slate-100 rounded transition-colors shrink-0">{open ? <ChevronDown className="w-3.5 h-3.5 text-slate-400" /> : <ChevronRight className="w-3.5 h-3.5 text-slate-400" />}</button>
        <button onClick={() => setSelectedNode({ key, displayName: name, sublabel, panelKind, levelLabel, isLeaf: false })} className={`flex-1 flex items-center gap-2 px-2 py-1.5 rounded-lg text-left transition-all ${isSelected ? 'bg-[rgb(26,77,156)] text-white' : 'hover:bg-slate-100'}`}>
          <Icon className={`w-3.5 h-3.5 shrink-0 ${isSelected ? 'text-white' : iconColor}`} />
          <span className={`text-xs font-semibold flex-1 truncate ${isSelected ? 'text-white' : 'text-slate-600'}`}>{name}</span>
          <span className={`text-[10px] ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}>{count}</span>
          {saved && <span className={`w-2 h-2 rounded-full shrink-0 ${isSelected ? 'bg-green-300' : 'bg-green-500'}`} />}
        </button>
      </div>
    );
  };

  const renderTeacherTree = () => {
    if (search) {
      const q = search.toLowerCase();
      return teachers.filter(t => t.name.toLowerCase().includes(q)).map(t => {
        const dept = departments.find(d => d.id === t.departmentId);
        return renderEntityBtn({ key: `t_${t.id}`, displayName: t.name, sublabel: dept?.name ?? '', panelKind: 'teacher_time', levelLabel: 'Преподаватель', isLeaf: true });
      });
    }
    return institutes.map(inst => {
      const instDepts = departments.filter(d => d.instituteId === inst.id);
      const instTeachers = teachers.filter(t => instDepts.some(d => d.id === t.departmentId));
      if (instTeachers.length === 0) return null;
      const open = isOpen(expandInstitutes, inst.id);
      return (
        <div key={inst.id}>
          {renderBranchBtn(`inst_t_${inst.id}`, 'teacher_time', 'Институт', inst.name, '', open, GraduationCap, 'text-[rgb(26,77,156)]', () => toggle(setExpandInstitutes, inst.id), instTeachers.length)}
          {open && <div className="ml-5 space-y-0.5">{instDepts.map(dept => { const deptTeachers = teachers.filter(t => t.departmentId === dept.id); if (!deptTeachers.length) return null; const dOpen = isOpen(expandDepts, dept.id); return <div key={dept.id}>{renderBranchBtn(`dept_${dept.id}`, 'teacher_time', 'Кафедра', dept.name, inst.name, dOpen, School, 'text-slate-500', () => toggle(setExpandDepts, dept.id), deptTeachers.length)}{dOpen && <div className="ml-5 space-y-0.5">{deptTeachers.map(t => renderEntityBtn({ key: `t_${t.id}`, displayName: t.name, sublabel: dept.name, panelKind: 'teacher_time', levelLabel: 'Преподаватель', isLeaf: true }))}</div>}</div>; })}</div>}
        </div>
      );
    });
  };

  const renderRoomTree = () => {
    if (search) {
      const q = search.toLowerCase();
      return mockRooms.filter(r => r.name.toLowerCase().includes(q)).map(r => { const bld = buildings.find(b => b.id === r.buildingId); return renderEntityBtn({ key: `r_${r.id}`, displayName: `Ауд. ${r.name}`, sublabel: bld?.name ?? '', panelKind: 'room', levelLabel: 'Аудитория', isLeaf: true, buildingId: r.buildingId }); });
    }
    return buildings.map(bld => {
      const bldRooms = mockRooms.filter(r => r.buildingId === bld.id);
      if (!bldRooms.length) return null;
      const open = isOpen(expandBuildings, bld.id);
      return <div key={bld.id}>{renderBranchBtn(`bld_${bld.id}`, 'room', 'Корпус', bld.name, bld.address, open, Building2, 'text-purple-600', () => toggle(setExpandBuildings, bld.id), bldRooms.length)}{open && <div className="ml-5 space-y-0.5">{bldRooms.map(r => renderEntityBtn({ key: `r_${r.id}`, displayName: `Ауд. ${r.name}`, sublabel: bld.name, panelKind: 'room', levelLabel: 'Аудитория', isLeaf: true, buildingId: bld.id }))}</div>}</div>;
    });
  };

  const renderGroupTree = () => {
    if (search) {
      const q = search.toLowerCase();
      return groups.filter(g => g.name.toLowerCase().includes(q)).map(g => { const inst = institutes.find(i => i.id === g.instituteId); return renderEntityBtn({ key: `g_${g.id}`, displayName: g.name, sublabel: `${inst?.name ?? ''} · ${EDU_LEVEL_LABELS[g.educationLevel ?? 'bachelor']} · ${g.course} курс`, panelKind: 'group_shift', levelLabel: 'Группа', isLeaf: true, instituteId: g.instituteId }); });
    }
    return institutes.map(inst => {
      const instGroups = groups.filter(g => g.instituteId === inst.id);
      if (!instGroups.length) return null;
      const open = isOpen(expandGroupInst, inst.id);
      const eduLevels = EDU_LEVEL_ORDER.filter(lv => instGroups.some(g => (g.educationLevel ?? 'bachelor') === lv));
      return (
        <div key={inst.id}>
          {renderBranchBtn(`inst_g_${inst.id}`, 'group_shift', 'Институт', inst.name, '', open, GraduationCap, 'text-teal-600', () => toggle(setExpandGroupInst, inst.id), instGroups.length)}
          {open && <div className="ml-5 space-y-0.5">{eduLevels.map(lv => { const lvGroups = instGroups.filter(g => (g.educationLevel ?? 'bachelor') === lv); const lvKey = `edl_${inst.id}_${lv}`; const lvOpen = isOpen(expandEduLevel, lvKey); const courses = [...new Set(lvGroups.map(g => g.course))].sort(); return <div key={lv}>{renderBranchBtn(lvKey, 'group_shift', 'Уровень образования', EDU_LEVEL_LABELS[lv], inst.name, lvOpen, Layers, 'text-teal-500', () => toggle(setExpandEduLevel, lvKey), lvGroups.length)}{lvOpen && <div className="ml-5 space-y-0.5">{courses.map(course => { const courseGroups = lvGroups.filter(g => g.course === course); const cKey = `course_${inst.id}_${lv}_${course}`; const cOpen = isOpen(expandCourse, cKey); return <div key={course}>{renderBranchBtn(cKey, 'group_shift', 'Курс', `${course} курс`, EDU_LEVEL_LABELS[lv], cOpen, Hash, 'text-slate-400', () => toggle(setExpandCourse, cKey), courseGroups.length)}{cOpen && <div className="ml-5 space-y-0.5">{courseGroups.map(g => renderEntityBtn({ key: `g_${g.id}`, displayName: g.name, sublabel: `${course} курс · ${g.size ?? 0} чел.`, panelKind: 'group_shift', levelLabel: 'Группа', isLeaf: true, instituteId: inst.id }))}</div>}</div>; })}</div>}</div>; })}</div>}
        </div>
      );
    });
  };

  const renderWorkloadList = () => (
    <div className="space-y-1">
      {filteredWorkload.length === 0 && <div className="text-center py-8 text-slate-400 text-sm"><LayoutList className="w-6 h-6 mx-auto mb-2 text-slate-300" />Нет записей</div>}
      {filteredWorkload.map(item => {
        const t = teachers.find(x => x.id === item.teacherId);
        const g = groups.find(x => x.id === item.groupId);
        const s = subjects.find(x => x.id === item.subjectId);
        const key = `wl_${item.id}`;
        const isSelected = selectedNode?.key === key;
        const saved = !!workloadConstraints[key];
        return (
          <button key={key} onClick={() => setSelectedNode({ key, displayName: s?.name ?? '—', sublabel: `${t?.name ?? '—'} · ${g?.name ?? '—'}`, panelKind: 'workload', levelLabel: 'Нагрузка', isLeaf: true, workloadItem: item })}
            className={`w-full flex items-start gap-2 px-3 py-2.5 rounded-lg text-left transition-all ${isSelected ? 'bg-[rgb(26,77,156)] text-white shadow-sm' : 'hover:bg-slate-100'}`}>
            <div className="flex-1 min-w-0">
              <div className={`text-xs font-semibold truncate ${isSelected ? 'text-white' : 'text-slate-800'}`}>{s?.name ?? '—'}</div>
              <div className={`text-[10px] truncate mt-0.5 ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}>{t?.name ?? '—'}</div>
              <div className="flex items-center gap-1.5 mt-1 flex-wrap">
                <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${isSelected ? 'bg-white/20 text-white' : LESSON_TYPE_COLORS[item.lessonType]}`}>{LESSON_TYPE_LABELS[item.lessonType]}</span>
                <span className={`text-[10px] ${isSelected ? 'text-blue-200' : 'text-slate-400'}`}>{g?.name} · {item.hoursPerWeek}ч/н</span>
                {item.subgroups.length > 0 && <span className={`text-[10px] ${isSelected ? 'text-blue-200' : 'text-purple-600'}`}>{item.subgroups.length} подгр.</span>}
              </div>
            </div>
            {saved && <span className={`w-2 h-2 rounded-full shrink-0 mt-1 ${isSelected ? 'bg-green-300' : 'bg-green-500'}`} />}
          </button>
        );
      })}
    </div>
  );

  const renderRightPanel = () => {
    if (activeTab === 'manage') {
      return <ManagePanel managedRooms={managedRooms} onRoomsChange={setManagedRooms} customEquipment={customEquipment} onEquipmentChange={setCustomEquipment} />;
    }
    if (!selectedNode) return <EmptyState onTabChange={t => { setActiveTab(t); setSelectedNode(null); }} />;

    if (selectedNode.panelKind === 'teacher_time') {
      const c = timeConstraints[selectedNode.key] ?? makeDefaultTeacher();
      return <TeacherPanel node={selectedNode} constraint={c} onChange={nc => setTimeConstraints(prev => ({ ...prev, [selectedNode.key]: nc }))} onSave={() => setTimeConstraints(prev => ({ ...prev, [selectedNode.key]: c }))} />;
    }
    if (selectedNode.panelKind === 'room') {
      const c = roomConstraints[selectedNode.key] ?? makeDefaultRoom();
      return <RoomPanel node={selectedNode} constraint={c} onChange={nc => setRoomConstraints(prev => ({ ...prev, [selectedNode.key]: nc }))} onSave={() => setRoomConstraints(prev => ({ ...prev, [selectedNode.key]: c }))} />;
    }
    if (selectedNode.panelKind === 'group_shift') {
      const c = groupConstraints[selectedNode.key] ?? makeDefaultGroup();
      return <GroupPanel node={selectedNode} constraint={c} onChange={nc => setGroupConstraints(prev => ({ ...prev, [selectedNode.key]: nc }))} onSave={() => setGroupConstraints(prev => ({ ...prev, [selectedNode.key]: c }))} />;
    }
    if (selectedNode.panelKind === 'workload' && selectedNode.workloadItem) {
      const item = selectedNode.workloadItem;
      const c = workloadConstraints[selectedNode.key] ?? makeDefaultWorkload(item);
      const handleMassApply = (scope: MassScope) => {
        let targetKeys: string[] = [];
        if (scope === 'item') targetKeys = [selectedNode.key];
        else if (scope === 'subject') targetKeys = workloadItems.filter(w => w.subjectId === item.subjectId).map(w => `wl_${w.id}`);
        else if (scope === 'teacher') targetKeys = workloadItems.filter(w => w.teacherId === item.teacherId).map(w => `wl_${w.id}`);
        else if (scope === 'group') targetKeys = workloadItems.filter(w => w.groupId === item.groupId).map(w => `wl_${w.id}`);
        else if (scope === 'institute') { const instGrps = groups.filter(g => g.instituteId === groups.find(x => x.id === item.groupId)?.instituteId).map(g => g.id); targetKeys = workloadItems.filter(w => instGrps.includes(w.groupId)).map(w => `wl_${w.id}`); }
        else targetKeys = workloadItems.map(w => `wl_${w.id}`);
        const updates: Record<string, WorkloadConstraintData> = {};
        targetKeys.forEach(k => { updates[k] = { ...c }; });
        setWorkloadConstraints(prev => ({ ...prev, ...updates }));
      };
      return <WorkloadPanel node={selectedNode} constraint={c} onChange={nc => setWorkloadConstraints(prev => ({ ...prev, [selectedNode.key]: nc }))} onSave={() => setWorkloadConstraints(prev => ({ ...prev, [selectedNode.key]: c }))} onMassApply={handleMassApply} />;
    }
    return null;
  };

  const TABS: { type: TabType; short: string; icon: React.FC<{ className?: string }> }[] = [
    { type: 'teacher',  short: 'Препод.', icon: User },
    { type: 'room',     short: 'Ауд.',    icon: MapPin },
    { type: 'group',    short: 'Группы',  icon: Users },
    { type: 'workload', short: 'Нагрузка',icon: BookOpen },
    { type: 'manage',   short: 'Объекты', icon: Database },
  ];

  const savedCount = {
    teacher: Object.keys(timeConstraints).length,
    room: Object.keys(roomConstraints).length,
    group: Object.keys(groupConstraints).length,
    workload: Object.keys(workloadConstraints).length,
    manage: managedRooms.length,
  };

  return (
    <EquipCtx.Provider value={{ allEquipment }}>
      <div className="flex h-full bg-slate-50 overflow-hidden rounded-xl border border-slate-200 shadow-sm">
        {/* Left panel */}
        <div className="w-72 shrink-0 flex flex-col border-r border-slate-200 bg-white">
          <div className="px-4 py-4 border-b border-slate-100">
            <div className="flex items-center gap-2 mb-3"><SlidersHorizontal className="w-4 h-4 text-[rgb(26,77,156)]" /><h2 className="text-sm font-bold text-slate-800">Ограничения</h2></div>
            <div className="relative"><Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-slate-400" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Поиск…" className="w-full pl-8 pr-3 py-2 text-sm border border-slate-200 rounded-lg bg-slate-50 focus:outline-none focus:ring-2 focus:ring-[rgb(26,77,156)]/30 focus:bg-white transition-all" /></div>
          </div>

          {/* Tabs */}
          <div className="flex border-b border-slate-100 bg-slate-50/60 shrink-0">
            {TABS.map(tab => {
              const TIcon = tab.icon;
              const cnt = savedCount[tab.type];
              return (
                <button key={tab.type} onClick={() => { setActiveTab(tab.type); setSearch(''); if (tab.type !== 'manage') setSelectedNode(null); }} title={tab.short}
                  className={`flex-1 flex flex-col items-center gap-0.5 py-2.5 text-[10px] font-medium transition-all border-b-2 relative ${activeTab === tab.type ? 'border-[rgb(26,77,156)] text-[rgb(26,77,156)] bg-white' : 'border-transparent text-slate-400 hover:text-slate-600 hover:bg-white/60'}`}>
                  <TIcon className="w-4 h-4" />
                  <span className="truncate max-w-full px-0.5">{tab.short}</span>
                  {cnt > 0 && tab.type !== 'manage' && <span className="absolute top-1 right-0.5 w-4 h-4 rounded-full bg-green-500 text-white text-[9px] flex items-center justify-center font-bold">{cnt > 9 ? '9+' : cnt}</span>}
                </button>
              );
            })}
          </div>

          {/* Workload filters */}
          {activeTab === 'workload' && (
            <div className="px-3 py-2.5 border-b border-slate-100 space-y-2 shrink-0 bg-slate-50/50">
              <div className="flex items-center gap-1.5 mb-1"><Filter className="w-3 h-3 text-slate-400" /><span className="text-[11px] text-slate-500 font-medium">Фильтры</span></div>
              <select value={wlTeacher} onChange={e => setWlTeacher(e.target.value)} className="w-full text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/30"><option value="">Все преподаватели</option>{teachers.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select>
              <select value={wlGroup} onChange={e => setWlGroup(e.target.value)} className="w-full text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/30"><option value="">Все группы</option>{groups.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}</select>
              <select value={wlSubject} onChange={e => setWlSubject(e.target.value)} className="w-full text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-1 focus:ring-[rgb(26,77,156)]/30"><option value="">Все дисциплины</option>{subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}</select>
            </div>
          )}

          {/* Count badge */}
          {activeTab !== 'manage' && (
            <div className="px-4 py-2 flex items-center justify-between border-b border-slate-100 shrink-0">
              <span className="text-[11px] text-slate-400">{activeTab === 'teacher' ? 'Преподаватели' : activeTab === 'room' ? 'Аудитории' : activeTab === 'group' ? 'Группы' : 'Нагрузка'}</span>
              <span className="text-[11px] bg-slate-100 text-slate-500 px-2 py-0.5 rounded-full">{activeTab === 'workload' ? `${filteredWorkload.length}/${workloadItems.length}` : activeTab === 'teacher' ? teachers.length : activeTab === 'room' ? mockRooms.length : groups.length}</span>
            </div>
          )}

          {/* Entity list */}
          {activeTab !== 'manage' && (
            <div className="flex-1 overflow-y-auto p-2 space-y-0.5">
              {activeTab === 'teacher' && renderTeacherTree()}
              {activeTab === 'room' && renderRoomTree()}
              {activeTab === 'group' && renderGroupTree()}
              {activeTab === 'workload' && renderWorkloadList()}
            </div>
          )}

          {activeTab === 'manage' && (
            <div className="flex-1 flex items-center justify-center p-4">
              <div className="text-center">
                <Database className="w-10 h-10 text-[rgb(26,77,156)]/30 mx-auto mb-2" />
                <p className="text-xs text-slate-400">Управление объектами системы</p>
                <div className="mt-3 space-y-1">
                  <div className="flex items-center justify-between text-[11px] text-slate-500"><span>Аудиторий:</span><span className="font-semibold">{managedRooms.length}</span></div>
                  <div className="flex items-center justify-between text-[11px] text-slate-500"><span>Типов оборудования:</span><span className="font-semibold">{allEquipment.length}</span></div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Right panel */}
        <div className="flex-1 overflow-hidden">
          {renderRightPanel()}
        </div>
      </div>
    </EquipCtx.Provider>
  );
}
